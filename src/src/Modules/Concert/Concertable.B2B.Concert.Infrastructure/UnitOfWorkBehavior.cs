using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Concert.Infrastructure;

internal interface IUnitOfWorkBehavior : IUnitOfWorkBehavior<ConcertTenantDbContext>;

internal sealed class UnitOfWorkBehavior(IUnitOfWork<ConcertTenantDbContext> unitOfWork)
    : UnitOfWorkBehavior<ConcertTenantDbContext>(unitOfWork), IUnitOfWorkBehavior;
