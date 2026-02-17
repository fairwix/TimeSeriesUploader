using FluentValidation;

namespace TimeSeriesUploader.Application.Validators;

public class CsvRow
{
    public DateTime Date { get; set; }
    public double ExecutionTime { get; set; }
    public double Value { get; set; }
}

public class CsvRowValidator : AbstractValidator<CsvRow>
{
    private static readonly DateTime MinDate = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public CsvRowValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty()
            .GreaterThanOrEqualTo(MinDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Date must be between 2000-01-01 and current UTC time.");

        RuleFor(x => x.ExecutionTime)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Execution time cannot be negative.");

        RuleFor(x => x.Value)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Value cannot be negative.");
    }
}