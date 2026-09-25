using Concertable.B2B.Concert.Infrastructure.Data;

namespace Concertable.B2B.Concert.Infrastructure;

internal interface IUnitOfWork : Concertable.DataAccess.Application.IUnitOfWork<ConcertDbContext>;

internal sealed class UnitOfWork(ConcertDbContext context)
    : Concertable.DataAccess.Infrastructure.UnitOfWork<ConcertDbContext>(context), IUnitOfWork;

internal interface IPrivilegedUnitOfWork :
    Concertable.DataAccess.Application.IUnitOfWork<ConcertPrivilegedDbContext>;

internal sealed class PrivilegedUnitOfWork(ConcertPrivilegedDbContext context)
    : Concertable.DataAccess.Infrastructure.UnitOfWork<ConcertPrivilegedDbContext>(context),
        IPrivilegedUnitOfWork;
