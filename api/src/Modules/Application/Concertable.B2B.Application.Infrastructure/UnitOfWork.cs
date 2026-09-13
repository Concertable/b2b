using Concertable.B2B.Application.Infrastructure.Data;

namespace Concertable.B2B.Application.Infrastructure;

internal interface IUnitOfWork : Concertable.DataAccess.Application.IUnitOfWork<ApplicationDbContext>;

internal sealed class UnitOfWork(ApplicationDbContext context)
    : Concertable.DataAccess.Infrastructure.UnitOfWork<ApplicationDbContext>(context), IUnitOfWork;
