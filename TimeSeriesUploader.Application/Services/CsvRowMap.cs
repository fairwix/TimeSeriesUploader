using CsvHelper.Configuration;
using TimeSeriesUploader.Application.Csv.Converters;
using TimeSeriesUploader.Application.Validators;

public sealed class CsvRowMap : ClassMap<CsvRow>
{
    public CsvRowMap()
    {
        Map(m => m.Date)
            .Name("Date")
            .TypeConverter<StrictUtcDateConverter>();

        Map(m => m.ExecutionTime).Name("ExecutionTime");
        Map(m => m.Value).Name("Value");
    }
}