using Moq;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TimeSeriesUploader.Application.Dtos;
using TimeSeriesUploader.Application.Interfaces;
using TimeSeriesUploader.Application.Services;
using TimeSeriesUploader.Application.Validators;
using TimeSeriesUploader.Domain.Entities;
using AutoMapper;
using System.Text;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.EntityFrameworkCore;
using TimeSeriesUploader.Tests.Helpers;
using ValidationException = FluentValidation.ValidationException;

namespace TimeSeriesUploader.Tests.Services;

public class CsvProcessingServiceTests
{
    private readonly TestAppDbContext _testContext;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IValidator<CsvRow>> _validatorMock;
    private readonly Mock<ILogger<CsvProcessingService>> _loggerMock;
    private readonly CsvProcessingService _sut;

    public CsvProcessingServiceTests()
    {
        _testContext = new TestAppDbContext();
        _mapperMock = new Mock<IMapper>();
        _validatorMock = new Mock<IValidator<CsvRow>>();
        _loggerMock = new Mock<ILogger<CsvProcessingService>>();
        
        _sut = new CsvProcessingService(
            _testContext,
            _mapperMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);
    }

    private IFormFile CreateCsvFile(string content, string fileName = "test.csv")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName);
    }

    [Fact]
    public async Task ProcessCsvAsync_ValidCsv_ShouldReturnUploadResultDto()
    {
        var csvContent = "Date;ExecutionTime;Value\n" +
                         "2023-01-01T10-00-00.0000Z;1.5;10.5\n" +
                         "2023-01-01T10-05-00.0000Z;2.0;20.5\n";
        var file = CreateCsvFile(csvContent);
        var fileName = "test.csv";

        _testContext.Clear();

        _validatorMock.Setup(x => x.ValidateAsync(It.IsAny<CsvRow>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mapperMock.Setup(x => x.Map<ResultDto>(It.IsAny<ResultRecord>()))
            .Returns((ResultRecord r) => new ResultDto { FileName = r.FileName });
        
        var result = await _sut.ProcessCsvAsync(fileName, file, CancellationToken.None);
        
        result.Should().NotBeNull();
        result.FileName.Should().Be(fileName);
        result.RowsProcessed.Should().Be(2);
        
        _testContext.ValuesList.Should().HaveCount(2);
        _testContext.ResultsList.Should().HaveCount(1);
    }

    [Fact]
    public async Task ProcessCsvAsync_WhenValidationFails_ShouldThrowAndRollback()
    {
        var csvContent = "Date;ExecutionTime;Value\n" +
                         "2023-01-01T10-00-00.0000Z;-1;10.5\n";
        var file = CreateCsvFile(csvContent);
        var fileName = "test.csv";

        _testContext.Clear();

        var validationFailures = new List<ValidationFailure>
        {
            new("ExecutionTime", "Execution time cannot be negative.")
        };
        
        _validatorMock.Setup(x => x.ValidateAsync(It.IsAny<CsvRow>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));
        
        Func<Task> act = async () => await _sut.ProcessCsvAsync(fileName, file, CancellationToken.None);
        
        await act.Should().ThrowAsync<ValidationException>();
        _testContext.ValuesList.Should().BeEmpty();
        _testContext.ResultsList.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessCsvAsync_WhenRowCountExceedsLimit_ShouldThrow()
    {
        var rows = Enumerable.Range(0, 10001)
            .Select(_ => "2023-01-01T10-00-00.0000Z;1;1")
            .ToList();
        var csvContent = "Date;ExecutionTime;Value\n" + string.Join("\n", rows);
        var file = CreateCsvFile(csvContent);
        var fileName = "test.csv";

        _testContext.Clear();

        _validatorMock.Setup(x => x.ValidateAsync(It.IsAny<CsvRow>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        
        Func<Task> act = async () => await _sut.ProcessCsvAsync(fileName, file, CancellationToken.None);
        
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Number of rows must be between 1 and 10000*");
        _testContext.ValuesList.Should().BeEmpty();
        _testContext.ResultsList.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessCsvAsync_WhenFileIsEmpty_ShouldThrow()
    {
        var file = CreateCsvFile("");
        var fileName = "test.csv";

        _testContext.Clear();
        
        Func<Task> act = async () => await _sut.ProcessCsvAsync(fileName, file, CancellationToken.None);
        
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task ProcessCsvAsync_WhenExceptionOccurs_ShouldRollbackAndRethrow()
    {
        var csvContent = "Date;ExecutionTime;Value\n2023-01-01T10-00-00.0000Z;1;1\n";
        var file = CreateCsvFile(csvContent);
        var fileName = "test.csv";
        
        var mockContext = new Mock<IAppDbContext>();
        
        var valuesList = new List<ValueRecord>();
        var resultsList = new List<ResultRecord>();
        
        var mockValues = new Mock<DbSet<ValueRecord>>();
        mockValues.As<IQueryable<ValueRecord>>().Setup(m => m.Provider).Returns(valuesList.AsQueryable().Provider);
        mockValues.As<IQueryable<ValueRecord>>().Setup(m => m.Expression).Returns(valuesList.AsQueryable().Expression);
        mockValues.As<IQueryable<ValueRecord>>().Setup(m => m.ElementType).Returns(valuesList.AsQueryable().ElementType);
        mockValues.As<IQueryable<ValueRecord>>().Setup(m => m.GetEnumerator()).Returns(() => valuesList.AsQueryable().GetEnumerator());
        mockValues.Setup(m => m.AddRangeAsync(It.IsAny<IEnumerable<ValueRecord>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ValueRecord>, CancellationToken>((items, _) => valuesList.AddRange(items))
            .Returns(Task.CompletedTask);
        
        var mockResults = new Mock<DbSet<ResultRecord>>();
        mockResults.As<IQueryable<ResultRecord>>().Setup(m => m.Provider).Returns(resultsList.AsQueryable().Provider);
        mockResults.As<IQueryable<ResultRecord>>().Setup(m => m.Expression).Returns(resultsList.AsQueryable().Expression);
        mockResults.As<IQueryable<ResultRecord>>().Setup(m => m.ElementType).Returns(resultsList.AsQueryable().ElementType);
        mockResults.As<IQueryable<ResultRecord>>().Setup(m => m.GetEnumerator()).Returns(() => resultsList.AsQueryable().GetEnumerator());
        
        mockContext.Setup(x => x.Values).Returns(mockValues.Object);
        mockContext.Setup(x => x.Results).Returns(mockResults.Object);
        
        mockContext.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));
        
        mockContext.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var serviceWithMock = new CsvProcessingService(
            mockContext.Object,
            _mapperMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);

        _validatorMock.Setup(x => x.ValidateAsync(It.IsAny<CsvRow>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        
        Func<Task> act = async () => await serviceWithMock.ProcessCsvAsync(fileName, file, CancellationToken.None);

        
        await act.Should().ThrowAsync<InvalidOperationException>();
        mockContext.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockContext.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessCsvAsync_WhenCancelled_ShouldThrow()
    {
        var csvContent = "Date;ExecutionTime;Value\n2023-01-01T10-00-00.0000Z;1;1\n";
        var file = CreateCsvFile(csvContent);
        var fileName = "test.csv";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _testContext.Clear();

        _validatorMock.Setup(x => x.ValidateAsync(It.IsAny<CsvRow>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        
        Func<Task> act = async () => await _sut.ProcessCsvAsync(fileName, file, cts.Token);
        
        await act.Should().ThrowAsync<OperationCanceledException>();
        _testContext.ValuesList.Should().BeEmpty();
        _testContext.ResultsList.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessCsvAsync_WhenFileExists_ShouldDeleteOldData()
    {
        var csvContent = "Date;ExecutionTime;Value\n" +
                         "2023-01-01T10-00-00.0000Z;1.5;10.5\n";
        var file = CreateCsvFile(csvContent);
        var fileName = "test.csv";

        _testContext.Clear();
        
        var oldValue = new ValueRecord 
        { 
            Id = Guid.NewGuid(), 
            FileName = fileName,
            Date = DateTime.UtcNow.AddDays(-1),
            ExecutionTime = 1,
            Value = 1
        };
        _testContext.ValuesList.Add(oldValue);
    
        var oldResult = new ResultRecord 
        { 
            FileName = fileName,
            TimeDeltaSeconds = 100,
            FirstExecutionDate = DateTime.UtcNow.AddDays(-1),
            AvgExecutionTime = 1,
            AvgValue = 1,
            MedianValue = 1,
            MaxValue = 1,
            MinValue = 1
        };
        _testContext.ResultsList.Add(oldResult);

        _validatorMock.Setup(x => x.ValidateAsync(It.IsAny<CsvRow>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mapperMock.Setup(x => x.Map<ResultDto>(It.IsAny<ResultRecord>()))
            .Returns((ResultRecord r) => new ResultDto { FileName = r.FileName });
        
        var result = await _sut.ProcessCsvAsync(fileName, file, CancellationToken.None);
        
        result.Should().NotBeNull();
        result.RowsProcessed.Should().Be(1);
        
        _testContext.ValuesList.Should().Contain(v => v.Value == 10.5);
        _testContext.ResultsList.Should().Contain(r => r.AvgValue == 10.5);
    }
}