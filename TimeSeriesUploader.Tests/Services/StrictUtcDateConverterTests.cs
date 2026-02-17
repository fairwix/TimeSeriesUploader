using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using FluentAssertions;
using System.Globalization;
using TimeSeriesUploader.Application.Csv.Converters;
using TimeSeriesUploader.Application.Validators;

namespace TimeSeriesUploader.Tests.Csv.Converters;

public class StrictUtcDateConverterTests
{
    private static CsvRow ReadSingleRow(string dateValue)
    {
        var csv = $"""
Date;ExecutionTime;Value
{dateValue};1.5;10
""";

        using var reader = new StringReader(csv);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = true
        };

        using var csvReader = new CsvReader(reader, config);
        csvReader.Context.RegisterClassMap<TestCsvRowMap>();

        csvReader.Read();
        csvReader.ReadHeader();
        csvReader.Read();

        return csvReader.GetRecord<CsvRow>();
    }

    [Fact]
    public void Parse_ValidDate_WithHyphens_ShouldParseSuccessfully()
    {
        var validDate = "2023-01-01T10-00-00.0000Z";
        
        var row = ReadSingleRow(validDate);
        
        row.Date.Should().Be(
            new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Parse_InvalidDate_WithColons_ShouldThrow()
    {
        var invalidDate = "2023-01-01T10:00:00.0000Z";
        
        Action act = () => ReadSingleRow(invalidDate);
        
        act.Should()
           .Throw<TypeConverterException>()
           .WithMessage("*Invalid Date format*");
    }

    [Fact]
    public void Parse_InvalidDate_WrongMilliseconds_ShouldThrow()
    {
        var invalidDate = "2023-01-01T10-00-00.000Z"; 
        
        Action act = () => ReadSingleRow(invalidDate);
        
        act.Should()
           .Throw<TypeConverterException>()
           .WithMessage("*Invalid Date format*");
    }

    [Fact]
    public void Parse_EmptyDate_ShouldThrow()
    {
        var invalidDate = "";
        
        Action act = () => ReadSingleRow(invalidDate);

        act.Should()
           .Throw<TypeConverterException>()
           .WithMessage("*Date value is missing*");
    }
    
    private sealed class TestCsvRowMap : ClassMap<CsvRow>
    {
        public TestCsvRowMap()
        {
            Map(m => m.Date)
                .Name("Date")
                .TypeConverter<StrictUtcDateConverter>();

            Map(m => m.ExecutionTime).Name("ExecutionTime");
            Map(m => m.Value).Name("Value");
        }
    }
}
