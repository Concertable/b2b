using Concertable.Shared.Email;
using Concertable.User.Contracts;
using Microsoft.Extensions.Logging;

namespace Concertable.User.Infrastructure.Services.Email;

internal sealed class AutoVerifyingFakeEmailSender : IEmailSender
{
    private readonly ILogger<AutoVerifyingFakeEmailSender> logger;
    private readonly IUserModule userModule;

    public AutoVerifyingFakeEmailSender(ILogger<AutoVerifyingFakeEmailSender> logger, IUserModule userModule)
    {
        this.logger = logger;
        this.userModule = userModule;
    }

    public Task SendEmailAsync(string toEmail, string subject, string body, IReadOnlyList<EmailAttachment>? attachments = null)
    {
        var count = attachments?.Count ?? 0;
        logger.LogInformation("[FakeEmail] To: {Email} | Subject: {Subject} | Attachments: {Count}\n{Body}", toEmail, subject, count, body);
        return Task.CompletedTask;
    }

    public async Task SendVerificationAsync(string toEmail, string token, string verifyBaseUrl, CancellationToken ct = default)
    {
        logger.LogInformation("[FakeEmail] Auto-verifying {Email}", toEmail);
        await userModule.VerifyEmailWithTokenAsync(token, ct);
    }
}
