namespace TimeSeriesUploader.Application.Dtos;

public class ValueDto
{
    public DateTime Date { get; set; }
    public double ExecutionTime { get; set; }
    public double Value { get; set; }
}