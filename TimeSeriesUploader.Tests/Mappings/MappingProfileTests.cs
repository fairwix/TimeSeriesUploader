using AutoMapper;
using TimeSeriesUploader.Application.Dtos;
using TimeSeriesUploader.Application.Mappings;
using TimeSeriesUploader.Domain.Entities;
using FluentAssertions;

namespace TimeSeriesUploader.Tests.Mappings;

public class MappingProfileTests
{
    private readonly IMapper _mapper;

    public MappingProfileTests()
    {
        var config = new MapperConfiguration(cfg => 
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void MappingProfile_Configuration_IsValid()
    {
        var config = new MapperConfiguration(cfg => 
        {
            cfg.AddProfile<MappingProfile>();
        });
        
        Action act = () => config.AssertConfigurationIsValid();
        act.Should().NotThrow();
    }

    [Fact]
    public void Map_ValueRecord_To_ValueDto_ShouldMapCorrectly()
    {
        var valueRecord = new ValueRecord
        {
            Id = Guid.NewGuid(),
            FileName = "test.csv",
            Date = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            ExecutionTime = 1.5,
            Value = 42.5
        };
        
        var valueDto = _mapper.Map<ValueDto>(valueRecord);
        
        valueDto.Date.Should().Be(valueRecord.Date);
        valueDto.ExecutionTime.Should().Be(valueRecord.ExecutionTime);
        valueDto.Value.Should().Be(valueRecord.Value);
    }

    [Fact]
    public void Map_ResultRecord_To_ResultDto_ShouldMapCorrectly()
    {
        var resultRecord = new ResultRecord
        {
            FileName = "test.csv",
            TimeDeltaSeconds = 300,
            FirstExecutionDate = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            AvgExecutionTime = 2.5,
            AvgValue = 50,
            MedianValue = 45,
            MaxValue = 100,
            MinValue = 10
        };
        
        var resultDto = _mapper.Map<ResultDto>(resultRecord);
        
        resultDto.FileName.Should().Be(resultRecord.FileName);
        resultDto.TimeDeltaSeconds.Should().Be(resultRecord.TimeDeltaSeconds);
        resultDto.FirstExecutionDate.Should().Be(resultRecord.FirstExecutionDate);
        resultDto.AvgExecutionTime.Should().Be(resultRecord.AvgExecutionTime);
        resultDto.AvgValue.Should().Be(resultRecord.AvgValue);
        resultDto.MedianValue.Should().Be(resultRecord.MedianValue);
        resultDto.MaxValue.Should().Be(resultRecord.MaxValue);
        resultDto.MinValue.Should().Be(resultRecord.MinValue);
    }
    
    [Fact]
    public void Map_ResultRecord_To_UploadResultDto_ShouldMapCorrectly()
    {
        var resultRecord = new ResultRecord
        {
            FileName = "test.csv",
            TimeDeltaSeconds = 300,
            FirstExecutionDate = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            AvgExecutionTime = 2.5,
            AvgValue = 50,
            MedianValue = 45,
            MaxValue = 100,
            MinValue = 10
        };
        
        var uploadResultDto = _mapper.Map<UploadResultDto>(resultRecord);
        
        uploadResultDto.FileName.Should().Be(resultRecord.FileName);
        uploadResultDto.AggregatedResults.Should().NotBeNull();
        uploadResultDto.RowsProcessed.Should().Be(0);
    }
}