using Microsoft.EntityFrameworkCore;
using TimeSeriesUploader.Domain.Entities;

namespace TimeSeriesUploader.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<ValueRecord> Values { get; }
    DbSet<ResultRecord> Results { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}