using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TimeSeriesUploader.Application.Interfaces;
using TimeSeriesUploader.Application.Mappings;
using TimeSeriesUploader.Application.Services;
using TimeSeriesUploader.Application.Validators;

namespace TimeSeriesUploader.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(MappingProfile));
        
        services.AddValidatorsFromAssemblyContaining<CsvRowValidator>();
        
        services.AddScoped<ICsvProcessingService, CsvProcessingService>();

        return services;
    }
}