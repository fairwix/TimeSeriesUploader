using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Globalization;

namespace TimeSeriesUploader.Application.Csv.Converters;

public sealed class StrictUtcDateConverter : DateTimeConverter
{
    private const string Format = "yyyy-MM-ddTHH-mm-ss.ffffZ";

    public override object ConvertFromString(
        string? text,
        IReaderRow row,
        MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new TypeConverterException(
                this,
                memberMapData,
                text,
                row.Context,
                "Date value is missing."
            );
        }

        if (!DateTime.TryParseExact(
                text,
                Format,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var date))
        {
            throw new TypeConverterException(
                this,
                memberMapData,
                text,
                row.Context,
                $"Invalid Date format. Expected '{Format}'."
            );
        }

        return date;
    }
}