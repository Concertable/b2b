using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Concert.Infrastructure;

internal interface IUnitOfWorkBehavior : IUnitOfWorkBehavior<TenantConcertDbContext>;

internal sealed class UnitOfWorkBehavior(IUnitOfWork<TenantConcertDbContext> unitOfWork)
    : UnitOfWorkBehavior<TenantConcertDbContext>(unitOfWork), IUnitOfWorkBehavior;
