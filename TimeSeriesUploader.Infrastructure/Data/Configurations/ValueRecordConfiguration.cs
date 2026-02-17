using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeSeriesUploader.Domain.Entities;

namespace TimeSeriesUploader.Infrastructure.Data.Configurations;

public class ValueRecordConfiguration : IEntityTypeConfiguration<ValueRecord>
{
    public void Configure(EntityTypeBuilder<ValueRecord> builder)
    {
        builder.ToTable("Values");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(x => x.Date)
            .IsRequired();
        
        builder.Property(x => x.ExecutionTime)
            .IsRequired()
            .HasPrecision(18, 6); 
        
        builder.Property(x => x.Value)
            .IsRequired()
            .HasPrecision(18, 6);
        
        builder.HasIndex(x => new { x.FileName, x.Date });
    }
}