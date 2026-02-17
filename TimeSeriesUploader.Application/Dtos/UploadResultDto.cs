namespace TimeSeriesUploader.Application.Dtos;

public class UploadResultDto
{
    public string FileName { get; set; } = string.Empty;
    public int RowsProcessed { get; set; }
    public ResultDto AggregatedResults { get; set; } = new();
}