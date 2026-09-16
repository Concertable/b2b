using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.Errors;

namespace Concertable.B2B.Application.Application.Mappers;

internal static class ApplicationShareMappers
{
    extension(ApplicationShareError error)
    {
        public ShareApplicationError ToShareApplicationError() => error switch
        {
            ApplicationShareError.ScopeNotShareable(var scope) => new ShareApplicationError.ScopeNotShareable(scope),
            ApplicationShareError.NotAPrincipal => new ShareApplicationError.NotAPrincipal(),
            ApplicationShareError.AlreadyShared => new ShareApplicationError.AlreadyShared()
        };
    }

    extension(ApplicationShareRevocationError error)
    {
        public RevokeApplicationShareError ToRevokeApplicationShareError() => error switch
        {
            ApplicationShareRevocationError.GrantNotFound => new RevokeApplicationShareError.GrantNotFound(),
            ApplicationShareRevocationError.NotAShare => new RevokeApplicationShareError.NotAShare(),
            ApplicationShareRevocationError.NotTheIssuer => new RevokeApplicationShareError.NotTheIssuer()
        };
    }

    extension(ApplicationAccessGrant grant)
    {
        public ApplicationShareResponse ToShareResponse() => new()
        {
            GrantId = grant.Id,
            ToTenantId = grant.TenantId,
            ToMemberUserId = grant.MemberUserId,
            Scope = grant.Scope,
            ValidFrom = grant.ValidFrom,
            ValidUntil = grant.ValidUntil
        };
    }
}
