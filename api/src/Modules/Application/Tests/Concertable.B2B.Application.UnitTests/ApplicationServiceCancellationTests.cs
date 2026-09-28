using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Application.Application.Mappers;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Infrastructure;
using Concertable.B2B.Application.Infrastructure.Services;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.Contracts.Enums;
using Concertable.Kernel.Identity;
using Moq;
using Reunion;

namespace Concertable.B2B.Application.UnitTests;

public sealed class ApplicationServiceCancellationTests
{
    [Fact]
    public async Task GetByOpportunityIdAsync_PropagatesCancellationToEveryRead()
    {
        var tenantId = Guid.NewGuid();
        var opportunityId = 7;
        using var cancellation = new CancellationTokenSource();
        var ct = cancellation.Token;
        var opportunity = new OpportunityDto(
            opportunityId,
            11,
            tenantId,
            13,
            DateTime.UnixEpoch,
            DateTime.UnixEpoch.AddDays(1),
            new HashSet<Genre>(),
            true);
        IReadOnlyList<ApplicationEntity> applications = [];
        IReadOnlyList<ApplicationProposalDto> proposals = [];
        var repository = new Mock<IApplicationRepository>();
        var opportunityModule = new Mock<IOpportunityModule>();
        var tenantContext = new Mock<ITenantContext>();
        var mapper = new Mock<IApplicationMapper>();
        opportunityModule
            .Setup(module => module.GetAsync(opportunityId, ct))
            .ReturnsAsync(Option.Some(opportunity));
        repository
            .Setup(value => value.GetByOpportunityIdAsync(opportunityId, ct))
            .ReturnsAsync(applications);
        mapper
            .Setup(value => value.ToProposalsAsync(applications, ct))
            .ReturnsAsync(proposals);
        tenantContext.SetupGet(value => value.TenantId).Returns(tenantId);
        var service = new ApplicationService(
            repository.Object,
            Mock.Of<IApplicationPrivilegedRepository>(),
            Mock.Of<IApplicationValidator>(),
            Mock.Of<IApplicationNotifier>(),
            Mock.Of<IApplicationWorkflow>(),
            Mock.Of<IApplicationEligibility>(),
            Mock.Of<IArtistModule>(),
            opportunityModule.Object,
            tenantContext.Object,
            Mock.Of<IApplicationCheckoutService>(),
            mapper.Object,
            TimeProvider.System,
            Mock.Of<IPrivilegedUnitOfWorkBehavior>(),
            Mock.Of<IMembershipContext>(),
            Mock.Of<IMembershipAuthorityFence>(),
            Mock.Of<IPermissionCatalog>(),
            Mock.Of<ICommandExecutor>());

        var result = await service.GetByOpportunityIdAsync(opportunityId, ct);

        Assert.True(result.TryGetValue(out var value));
        Assert.Same(proposals, value);
        opportunityModule.Verify(module => module.GetAsync(opportunityId, ct), Times.Once);
        repository.Verify(value => value.GetByOpportunityIdAsync(opportunityId, ct), Times.Once);
        mapper.Verify(value => value.ToProposalsAsync(applications, ct), Times.Once);
    }
}
