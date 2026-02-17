namespace TimeSeriesUploader.Domain.Entities;

public class ResultRecord
{
    public string FileName { get; set; } = null!;         
    public double TimeDeltaSeconds { get; set; }          
    public DateTime FirstExecutionDate { get; set; }       
    public double AvgExecutionTime { get; set; }           
    public double AvgValue { get; set; }                  
    public double MedianValue { get; set; }               
    public double MaxValue { get; set; }                  
    public double MinValue { get; set; }                  
}