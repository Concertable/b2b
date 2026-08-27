using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.DataAccess.Application;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure;

internal interface IUnitOfWorkBoundary : Concertable.DataAccess.Application.IUnitOfWorkBoundary<ConcertDbContext>;

internal sealed class FactoryUnitOfWork(IDbContextFactory<ConcertDbContext> dbContextFactory)
    : Concertable.DataAccess.Infrastructure.FactoryUnitOfWork<ConcertDbContext>(dbContextFactory),
        IUnitOfWorkBoundary;