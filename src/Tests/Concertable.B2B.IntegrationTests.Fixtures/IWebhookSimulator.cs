namespace Concertable.B2B.IntegrationTests.Fixtures;

public interface IWebhookSimulator
{
    Task SendWebhookAsync();
}
