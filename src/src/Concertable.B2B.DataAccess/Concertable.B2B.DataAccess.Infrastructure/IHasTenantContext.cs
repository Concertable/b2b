using Concertable.Kernel.Identity;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// A DbContext that holds the request's <see cref="ITenantContext"/>, so tenant query filters can
/// read the tenant THROUGH the context instance (see <see cref="TenantFilters"/>).
/// </summary>
public interface IHasTenantContext
{
    ITenantContext TenantContext { get; }
}
