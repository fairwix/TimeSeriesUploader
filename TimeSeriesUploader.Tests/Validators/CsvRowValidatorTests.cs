using FluentValidation.TestHelper;
using TimeSeriesUploader.Application.Validators;
using FluentAssertions;

namespace TimeSeriesUploader.Tests.Validators;

public class CsvRowValidatorTests
{
    private readonly CsvRowValidator _validator = new();

    [Fact]
    public void Validate_ValidRow_ShouldNotHaveErrors()
    {
        var row = new CsvRow
        {
            Date = DateTime.UtcNow.AddDays(-1),
            ExecutionTime = 5.5,
            Value = 123.45
        };
        
        var result = _validator.TestValidate(row);
        
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_DateBefore2000_ShouldHaveError()
    {
        var row = new CsvRow 
        { 
            Date = new DateTime(1999, 12, 31), 
            ExecutionTime = 1, 
            Value = 1 
        };
        
        var result = _validator.TestValidate(row);
        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Date");
    }

    [Fact]
    public void Validate_DateInFuture_ShouldHaveError()
    {
        var row = new CsvRow 
        { 
            Date = DateTime.UtcNow.AddDays(1), 
            ExecutionTime = 1, 
            Value = 1 
        };
        var result = _validator.TestValidate(row);
        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Date");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(-5)]
    public void Validate_NegativeExecutionTime_ShouldHaveError(double executionTime)
    {
        var row = new CsvRow 
        { 
            Date = DateTime.UtcNow, 
            ExecutionTime = executionTime, 
            Value = 1 
        };
        
        var result = _validator.TestValidate(row);
        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExecutionTime");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(-100)]
    public void Validate_NegativeValue_ShouldHaveError(double value)
    {
        var row = new CsvRow 
        { 
            Date = DateTime.UtcNow, 
            ExecutionTime = 1, 
            Value = value 
        };
        
        var result = _validator.TestValidate(row);
        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Value");
    }

    [Fact]
    public void Validate_EmptyDate_ShouldHaveError()
    {
        var row = new CsvRow 
        { 
            Date = default, 
            ExecutionTime = 1, 
            Value = 1 
        };
        
        var result = _validator.TestValidate(row);
        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Date");
    }
}