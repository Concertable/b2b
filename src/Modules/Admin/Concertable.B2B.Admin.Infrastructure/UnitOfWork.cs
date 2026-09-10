using Concertable.B2B.Admin.Infrastructure.Data;

namespace Concertable.B2B.Admin.Infrastructure;

internal interface IUnitOfWork : Concertable.DataAccess.Application.IUnitOfWork<AdminDbContext>;

internal sealed class UnitOfWork(AdminDbContext context)
    : Concertable.DataAccess.Infrastructure.UnitOfWork<AdminDbContext>(context), IUnitOfWork;
