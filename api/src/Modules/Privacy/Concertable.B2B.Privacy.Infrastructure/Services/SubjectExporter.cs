using System.Net.Mime;
using System.Text.Json;

namespace Concertable.B2B.Privacy.Infrastructure.Services;

internal sealed class SubjectExporter : ISubjectExporter
{
    private readonly IUserModule userModule;
    private readonly ITenantModule tenantModule;
    private readonly IConversationsModule conversationsModule;
    private readonly IBookingModule bookingModule;
    private readonly IConcertModule concertModule;

    public SubjectExporter(
        IUserModule userModule,
        ITenantModule tenantModule,
        IConversationsModule conversationsModule,
        IBookingModule bookingModule,
        IConcertModule concertModule)
    {
        this.userModule = userModule;
        this.tenantModule = tenantModule;
        this.conversationsModule = conversationsModule;
        this.bookingModule = bookingModule;
        this.concertModule = concertModule;
    }

    public async Task<FileDownload> ExportAsync(Guid subjectId, CancellationToken ct = default)
    {
        var user = await this.userModule.GetSubjectProfileAsync(subjectId, ct);
        var memberships = await this.tenantModule.GetMembershipsAsync(subjectId, ct);
        var tenantIds = memberships.Select(m => m.TenantId).ToHashSet();
        var messages = await this.conversationsModule.GetSubjectMessagesAsync(subjectId, ct);
        var contracts = await this.bookingModule.GetSubjectContractsAsync(tenantIds, ct);
        var concertRecords = await this.concertModule.GetSubjectRecordsAsync(tenantIds, ct);

        var payload = new
        {
            subjectId,
            user = user.ToNullable(),
            memberships,
            messages,
            contracts,
            concertRecords,
        };

        var content = JsonSerializer.SerializeToUtf8Bytes(payload, SubjectExportSerializerOptions.Value);
        return new FileDownload(content, $"subject-export-{subjectId:N}.json", MediaTypeNames.Application.Json);
    }
}
