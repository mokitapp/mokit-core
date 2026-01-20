using Mokit.Application.DTOs.Webhook;
using Mokit.Domain.Entities;

namespace Mokit.Application.Interfaces;

public interface IWebhookJobQueue
{
    ValueTask EnqueueAsync(WebhookJob job, CancellationToken cancellationToken = default);
    ValueTask<WebhookJob> DequeueAsync(CancellationToken cancellationToken);
}

public class WebhookJob
{
    public WebhookDefinition Definition { get; set; } = null!;
    public WebhookExecutionContext Context { get; set; } = null!;
    public Guid OriginalRequestId { get; set; }
}
