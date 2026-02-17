using AutoMapper;
using TimeSeriesUploader.Application.Dtos;
using TimeSeriesUploader.Domain.Entities;

namespace TimeSeriesUploader.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ValueRecord, ValueDto>();

        CreateMap<ResultRecord, ResultDto>()
            .ForMember(dest => dest.TimeDeltaSeconds, opt => opt.MapFrom(src => src.TimeDeltaSeconds))
            .ForMember(dest => dest.FirstExecutionDate, opt => opt.MapFrom(src => src.FirstExecutionDate));

        CreateMap<ResultRecord, UploadResultDto>()
            .ForMember(dest => dest.AggregatedResults, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.RowsProcessed, opt => opt.Ignore());
    }
}