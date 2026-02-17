using Microsoft.AspNetCore.Http;
using TimeSeriesUploader.Application.Dtos;

namespace TimeSeriesUploader.Application.Interfaces;

public interface ICsvProcessingService
{
    Task<UploadResultDto> ProcessCsvAsync(string fileName, IFormFile file, CancellationToken cancellationToken);
}