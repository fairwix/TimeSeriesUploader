using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeSeriesUploader.Domain.Entities;

namespace TimeSeriesUploader.Infrastructure.Data.Configurations;

public class ResultRecordConfiguration : IEntityTypeConfiguration<ResultRecord>
{
    public void Configure(EntityTypeBuilder<ResultRecord> builder)
    {
        builder.ToTable("Results");
        builder.HasKey(x => x.FileName);                 

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.TimeDeltaSeconds)
            .IsRequired()
            .HasPrecision(18, 6);

        builder.Property(x => x.FirstExecutionDate)
            .IsRequired();

        builder.Property(x => x.AvgExecutionTime)
            .HasPrecision(18, 6);

        builder.Property(x => x.AvgValue)
            .HasPrecision(18, 6);

        builder.Property(x => x.MedianValue)
            .HasPrecision(18, 6);

        builder.Property(x => x.MaxValue)
            .HasPrecision(18, 6);

        builder.Property(x => x.MinValue)
            .HasPrecision(18, 6);
    }
}