using Concertable.B2B.Admin.Application.Interfaces;

namespace Concertable.B2B.Admin.Infrastructure;

internal sealed class AdminModule : IAdminModule
{
    private readonly IAdminService service;

    public AdminModule(IAdminService service)
    {
        this.service = service;
    }

    public Task<bool> IsCurrentUserAdminAsync(CancellationToken ct = default) =>
        service.IsCurrentUserAdminAsync(ct);
}
