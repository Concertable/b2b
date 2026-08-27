using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Application;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure;

internal interface IUnitOfWorkBoundary : IUnitOfWorkBoundary<ConcertDbContext>;

internal sealed class UnitOfWorkBoundary(IDbContextFactory<ConcertDbContext> dbContextFactory)
    : Concertable.DataAccess.Infrastructure.UnitOfWorkBoundary<ConcertDbContext>(dbContextFactory),
        IUnitOfWorkBoundary;