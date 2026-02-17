using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeSeriesUploader.Application.Dtos;
using TimeSeriesUploader.Application.Interfaces;

namespace TimeSeriesUploader.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResultsController : ControllerBase
{
    private readonly IAppDbContext _context;
    private readonly IMapper _mapper;

    public ResultsController(IAppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    /// <summary>
    /// метод 2: получение списка записей из таблицы Results с фильтрацией
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ResultDto>>> GetResults(
        [FromQuery] string? fileName,
        [FromQuery] DateTime? minDateFrom,
        [FromQuery] DateTime? minDateTo,
        [FromQuery] double? avgValueFrom,
        [FromQuery] double? avgValueTo,
        [FromQuery] double? avgExecutionTimeFrom,
        [FromQuery] double? avgExecutionTimeTo,
        CancellationToken cancellationToken)
    {
        IQueryable<Domain.Entities.ResultRecord> query = _context.Results.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(fileName))
            query = query.Where(r => EF.Functions.ILike(r.FileName, fileName));
        
        if (minDateFrom.HasValue)
            query = query.Where(r => r.FirstExecutionDate >= minDateFrom.Value);
        
        if (minDateTo.HasValue)
            query = query.Where(r => r.FirstExecutionDate <= minDateTo.Value);

        if (avgValueFrom.HasValue)
            query = query.Where(r => r.AvgValue >= avgValueFrom.Value);
        
        if (avgValueTo.HasValue)
            query = query.Where(r => r.AvgValue <= avgValueTo.Value);

        if (avgExecutionTimeFrom.HasValue)
            query = query.Where(r => r.AvgExecutionTime >= avgExecutionTimeFrom.Value);
        
        if (avgExecutionTimeTo.HasValue)
            query = query.Where(r => r.AvgExecutionTime <= avgExecutionTimeTo.Value);

        var results = await query.ToListAsync(cancellationToken);
        return Ok(_mapper.Map<IEnumerable<ResultDto>>(results));
    }
}