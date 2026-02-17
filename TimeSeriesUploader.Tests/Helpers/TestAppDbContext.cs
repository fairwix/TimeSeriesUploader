using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using System.Linq.Expressions;
using TimeSeriesUploader.Application.Interfaces;
using TimeSeriesUploader.Domain.Entities;

namespace TimeSeriesUploader.Tests.Helpers;

public class TestAppDbContext : IAppDbContext
{
    private readonly List<ValueRecord> _values = new();
    private readonly List<ResultRecord> _results = new();
    private bool _inTransaction;

    public DbSet<ValueRecord> Values 
    { 
        get
        {
            var mockSet = CreateMockDbSet(_values);
            
            mockSet.Setup(m => m.AddRangeAsync(It.IsAny<IEnumerable<ValueRecord>>(), It.IsAny<CancellationToken>()))
                .Callback<IEnumerable<ValueRecord>, CancellationToken>((items, _) => _values.AddRange(items))
                .Returns(Task.CompletedTask);
            
            mockSet.Setup(m => m.RemoveRange(It.IsAny<IEnumerable<ValueRecord>>()))
                .Callback<IEnumerable<ValueRecord>>(items =>
                {
                    foreach (var item in items.ToList())
                        _values.Remove(item);
                });
                
            return mockSet.Object;
        }
        set => throw new NotImplementedException();
    }

    public DbSet<ResultRecord> Results 
    { 
        get
        {
            var mockSet = CreateMockDbSet(_results);
            
            mockSet.Setup(m => m.AddAsync(It.IsAny<ResultRecord>(), It.IsAny<CancellationToken>()))
                .Callback<ResultRecord, CancellationToken>((item, _) => _results.Add(item))
                .Returns((ResultRecord item, CancellationToken token) => 
                    new ValueTask<EntityEntry<ResultRecord>>(default(EntityEntry<ResultRecord>)!));
            
            mockSet.Setup(m => m.FindAsync(It.IsAny<object[]>()))
                .Returns<object[]>(ids => 
                {
                    var fileName = ids[0] as string;
                    var result = _results.FirstOrDefault(r => r.FileName == fileName);
                    return new ValueTask<ResultRecord?>(result);
                });
            
            mockSet.Setup(m => m.Remove(It.IsAny<ResultRecord>()))
                .Callback<ResultRecord>(item => _results.Remove(item))
                .Returns((ResultRecord item) => default(EntityEntry<ResultRecord>)!);
                
            return mockSet.Object;
        }
        set => throw new NotImplementedException();
    }

    private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> list) where T : class
    {
        var queryable = list.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();
        
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(queryable.Provider));
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
        
        mockSet.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));
        
        mockSet.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(list.Add);
        mockSet.Setup(m => m.Remove(It.IsAny<T>())).Callback<T>(item => list.Remove(item));
        
        return mockSet;
    }

    public List<ValueRecord> ValuesList => _values;
    public List<ResultRecord> ResultsList => _results;
    public bool IsInTransaction => _inTransaction;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_values.Count + _results.Count);
    }

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _inTransaction = true;
        return Task.CompletedTask;
    }

    public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        _inTransaction = false;
        return Task.CompletedTask;
    }

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        _inTransaction = false;
        return Task.CompletedTask;
    }

    public void Clear()
    {
        _values.Clear();
        _results.Clear();
        _inTransaction = false;
    }
}

internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object? Execute(Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var expectedResultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = ((IQueryProvider)this).Execute(expression);
        
        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
            ?.MakeGenericMethod(expectedResultType)
            .Invoke(null, new[] { executionResult })!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public T Current => _inner.Current;

    public ValueTask<bool> MoveNextAsync()
    {
        return ValueTask.FromResult(_inner.MoveNext());
    }

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }
}