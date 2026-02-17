using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeSeriesUploader.Application.Dtos;
using TimeSeriesUploader.Application.Interfaces;

namespace TimeSeriesUploader.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly ICsvProcessingService _csvProcessingService;
    private readonly IAppDbContext _context;
    private readonly IMapper _mapper;

    public FilesController(
        ICsvProcessingService csvProcessingService,
        IAppDbContext context,
        IMapper mapper)
    {
        _csvProcessingService = csvProcessingService;
        _mapper = mapper;
        _context = context;
    }

    /// <summary>
    /// метод 1: загрузка CSV-файла, обработка и сохранение
    /// </summary>
    /// <param name="file">CSV-файл с разделителем ;</param>
    /// <param name="cancellationToken"></param>
    /// <returns>результаты обработки</returns>
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(UploadResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UploadResultDto>> UploadCsv(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest("File is empty.");

        var fileName = Path.GetFileName(file.FileName);
        var result = await _csvProcessingService.ProcessCsvAsync(fileName, file, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// метод 3: получение последних 10 значений по имени файла
    /// </summary>
    /// <param name="fileName">имя файла</param>
    /// <param name="cancellationToken"></param>
    /// <returns>список из 10 записей, отсортированных по Date возрастанию</returns>
    [HttpGet("{fileName}/last10")]
    [ProducesResponseType(typeof(IEnumerable<ValueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ValueDto>>> GetLast10Values(
        string fileName,
        CancellationToken cancellationToken)
    {
        var values = await _context.Values
            .Where(v => v.FileName == fileName)
            .OrderByDescending(v => v.Date)
            .Take(10)
            .OrderBy(v => v.Date)
            .ToListAsync(cancellationToken);
        if (!values.Any())
            return NotFound($"No values found for file '{fileName}'.");

        return Ok(_mapper.Map<IEnumerable<ValueDto>>(values));
    }
}