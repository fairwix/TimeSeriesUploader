namespace TimeSeriesUploader.Domain.Entities;

public class ValueRecord
{
    public Guid Id { get; set; }             
    public string FileName { get; set; } = null!;
    public DateTime Date { get; set; }
    public double ExecutionTime { get; set; }
    public double Value { get; set; }
}