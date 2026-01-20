using System.Threading.Channels;
using Mokit.Application.Interfaces;

namespace Mokit.Infrastructure.Services;

public class WebhookJobQueue : IWebhookJobQueue
{
    private readonly Channel<WebhookJob> _queue;

    public WebhookJobQueue()
    {
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<WebhookJob>(options);
    }

    public async ValueTask EnqueueAsync(WebhookJob job, CancellationToken cancellationToken = default)
    {
        await _queue.Writer.WriteAsync(job, cancellationToken);
    }

    public async ValueTask<WebhookJob> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
