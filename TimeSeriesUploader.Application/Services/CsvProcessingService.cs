using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TimeSeriesUploader.Application.Dtos;
using TimeSeriesUploader.Application.Interfaces;
using TimeSeriesUploader.Application.Validators;
using TimeSeriesUploader.Domain.Entities;
using MissingFieldException = CsvHelper.MissingFieldException;
using ValidationException = FluentValidation.ValidationException; 
using Microsoft.Extensions.Logging;

namespace TimeSeriesUploader.Application.Services;

public class CsvProcessingService : ICsvProcessingService
{
    private readonly IAppDbContext _context;
    private readonly IMapper _mapper;
    private readonly IValidator<CsvRow> _rowValidator;
    private readonly ILogger<CsvProcessingService> _logger;

    public CsvProcessingService(
        IAppDbContext context,
        IMapper mapper,
        IValidator<CsvRow> rowValidator, ILogger<CsvProcessingService> logger)
    {
        _context = context;
        _mapper = mapper;
        _rowValidator = rowValidator;
        _logger = logger;
    }

    public async Task<UploadResultDto> ProcessCsvAsync(
        string fileName,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting processing of file {FileName}, size {FileSize} bytes", fileName, file.Length);
        await _context.BeginTransactionAsync(cancellationToken);

        try
        {
            var (records, errors) =
                await ParseAndValidateCsvAsync(fileName, file, cancellationToken);

            if (errors.Any())
            {
                _logger.LogWarning("CSV validation failed for {FileName}: {Errors}", fileName, string.Join("; ", errors));
                throw new ValidationException(
                    $"CSV validation failed: {string.Join("; ", errors)}");
            }

            if (records.Count < 1 || records.Count > 10000)
            {
                _logger.LogWarning("Invalid row count {RowCount} for file {FileName}", records.Count, fileName);
                throw new ValidationException(
                    "Number of rows must be between 1 and 10000.");
            }
            
            _logger.LogInformation("Parsed {RowCount} valid rows from {FileName}", records.Count, fileName);
            
            cancellationToken.ThrowIfCancellationRequested();
            
            await DeleteExistingDataAsync(fileName, cancellationToken);
            
            await _context.Values.AddRangeAsync(records, cancellationToken);

            var aggregatedResult = ComputeAggregates(fileName, records);
            await _context.Results.AddAsync(aggregatedResult, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await _context.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Successfully saved {RowCount} rows for file {FileName}", records.Count, fileName);

            return new UploadResultDto
            {
                FileName = fileName,
                RowsProcessed = records.Count,
                AggregatedResults = _mapper.Map<ResultDto>(aggregatedResult)
            };
        }
        catch(OperationCanceledException)
        {
            _logger.LogInformation("Processing cancelled for file {FileName}", fileName);
            await _context.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file {FileName}", fileName);
            await _context.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task DeleteExistingDataAsync(string fileName, CancellationToken cancellationToken)
    {
        var existingValues = await _context.Values
            .Where(v => v.FileName == fileName)
            .ToListAsync(cancellationToken);
        
        if (existingValues.Any())
        {
            _context.Values.RemoveRange(existingValues);
            _logger.LogDebug("Marked {Count} existing value records for deletion", existingValues.Count);
        }
        
        var existingResult = await _context.Results
            .FindAsync(new object[] { fileName }, cancellationToken);
        
        if (existingResult != null)
        {
            _context.Results.Remove(existingResult);
            _logger.LogDebug("Marked existing result record for deletion");
        }
    }
    
    private async Task<(List<ValueRecord> Records, List<string> Errors)>
    ParseAndValidateCsvAsync(
        string fileName,
        IFormFile file,
        CancellationToken cancellationToken)
{
    var records = new List<ValueRecord>();
    var errors = new List<string>();

    var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        Delimiter = ";",
        HasHeaderRecord = true,

        MissingFieldFound = args =>
        {
            int row = args.Context!.Parser!.Row;
            throw new CsvHelper.MissingFieldException(
                args.Context,
                $"Missing field at row {row}: {string.Join(", ", args.HeaderNames ?? Array.Empty<string>())}"
            );
        },

        BadDataFound = args =>
        {
            throw new CsvHelper.BadDataException(
                string.Empty,
                args.RawRecord ?? string.Empty,
                args.Context
            );
        }

    };

    await using var stream = file.OpenReadStream();
    using var reader = new StreamReader(stream);
    using var csv = new CsvReader(reader, csvConfig);

    csv.Context.RegisterClassMap<CsvRowMap>();

    await csv.ReadAsync();
    csv.ReadHeader();

    var rowNumber = 1;

    while (await csv.ReadAsync())
    {
        cancellationToken.ThrowIfCancellationRequested();
        rowNumber++;

        CsvRow row;
        try
        {
            row = csv.GetRecord<CsvRow>();
        }
        catch (MissingFieldException ex)
        {
            errors.Add($"Row {rowNumber}: {ex.Message}");
            continue;
        }
        catch (BadDataException ex)
        {
            errors.Add($"Row {rowNumber}: {ex.Message}");
            continue;
        }
        catch (TypeConverterException ex)
        {
            errors.Add($"Row {rowNumber}: Invalid value format - {ex.Message}");
            continue;
        }
        catch (Exception ex)
        {
            errors.Add($"Row {rowNumber}: Failed to parse - {ex.Message}");
            continue;
        }

        var validationResult = await _rowValidator.ValidateAsync(row, cancellationToken);

        if (!validationResult.IsValid)
        {
            errors.Add(
                $"Row {rowNumber}: " +
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))
            );
            continue;
        }

        records.Add(new ValueRecord
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            Date = DateTime.SpecifyKind(row.Date, DateTimeKind.Utc),
            ExecutionTime = row.ExecutionTime,
            Value = row.Value
        });
    }

    return (records, errors);
}

    private ResultRecord ComputeAggregates(
        string fileName,
        List<ValueRecord> records)
    {
        var dates = records.Select(r => r.Date).ToList();
        var minDate = dates.Min();
        var maxDate = dates.Max();

        var executionTimes = records.Select(r => r.ExecutionTime).ToList();
        var values = records.Select(r => r.Value).OrderBy(v => v).ToList();

        return new ResultRecord
        {
            FileName = fileName,
            TimeDeltaSeconds = (maxDate - minDate).TotalSeconds,
            FirstExecutionDate = minDate,
            AvgExecutionTime = executionTimes.Average(),
            AvgValue = values.Average(),
            MedianValue = ComputeMedian(values),
            MaxValue = values.Max(),
            MinValue = values.Min()
        };
    }

    private static double ComputeMedian(List<double> sortedValues)
    {
        int n = sortedValues.Count;

        return n % 2 == 0
            ? (sortedValues[n / 2 - 1] + sortedValues[n / 2]) / 2.0
            : sortedValues[n / 2];
    }
}

