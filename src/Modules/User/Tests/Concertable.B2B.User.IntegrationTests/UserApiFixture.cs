using Concertable.Auth.Contracts.Events;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.User.Domain.Entities;
using Concertable.B2B.User.Infrastructure.Data;
using Concertable.B2B.User.Infrastructure.Events;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.User.IntegrationTests;

public sealed class UserApiFixture : ApiFixture
{
    private UserDbContext dbContext = null!;
    private CredentialRegisteredHandler provisioning = null!;

    public IQueryable<UserEntity> Users => dbContext.Users.AsNoTracking();

    public Task ProvisionAsync(CredentialRegisteredEvent @event, MessageEnvelope? envelope = null) =>
        provisioning.HandleAsync(
            @event,
            envelope ?? MessageEnvelope.Create<CredentialRegisteredEvent>(DateTimeOffset.UtcNow));

    public Task<bool> UserExistsAsync(Guid userId) =>
        dbContext.Users.AnyAsync(user => user.Id == userId);

    protected override void OnReset(IServiceScope scope)
    {
        dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        provisioning = scope.ServiceProvider
            .GetServices<IIntegrationEventHandler<CredentialRegisteredEvent>>()
            .OfType<CredentialRegisteredHandler>()
            .Single();
    }
}
