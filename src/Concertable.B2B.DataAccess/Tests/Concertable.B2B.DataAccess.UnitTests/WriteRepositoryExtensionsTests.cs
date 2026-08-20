using Concertable.B2B.DataAccess.Infrastructure.Extensions;
using Concertable.DataAccess.Application;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.UnitTests;

public sealed class WriteRepositoryExtensionsTests
{
    [Fact]
    public async Task TryInsertAsync_Success_ReturnsInsertedEntity()
    {
        var entity = new TestEntity();
        var repository = new TestRepository(entity);

        var result = await repository.TryInsertAsync(entity, CancellationToken.None);

        Assert.True(result.TryGetValue(out var inserted));
        Assert.Same(entity, inserted);
    }

    [Fact]
    public async Task TryInsertAsync_NonDuplicateFailure_PropagatesException()
    {
        var exception = new DbUpdateException();
        var repository = new TestRepository(exception);

        var actual = await Assert.ThrowsAsync<DbUpdateException>(
            () => repository.TryInsertAsync(new TestEntity(), CancellationToken.None));

        Assert.Same(exception, actual);
    }

    private sealed class TestRepository : IWriteRepository<TestEntity>
    {
        private readonly TestEntity? inserted;
        private readonly Exception? exception;

        public TestRepository(TestEntity inserted)
        {
            this.inserted = inserted;
        }

        public TestRepository(Exception exception)
        {
            this.exception = exception;
        }

        public Task<TestEntity> InsertAsync(TestEntity entity, CancellationToken ct = default) =>
            exception is null
                ? Task.FromResult(inserted!)
                : Task.FromException<TestEntity>(exception);

        public Task<TestEntity> AddAsync(TestEntity entity, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IEnumerable<TestEntity>> AddRangeAsync(
            IEnumerable<TestEntity> entities,
            CancellationToken ct = default) =>
            throw new NotSupportedException();

        public void Update(TestEntity entity) => throw new NotSupportedException();

        public void Remove(TestEntity entity) => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class TestEntity;
}
