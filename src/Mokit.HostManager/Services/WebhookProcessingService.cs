using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mokit.Application.Interfaces;
using Mokit.MockEngine.Templates;

namespace Mokit.HostManager.Services;

public class WebhookProcessingService : BackgroundService
{
    private readonly IWebhookJobQueue _queue;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookProcessingService> _logger;
    private readonly TemplateEngine _templateEngine;

    public WebhookProcessingService(
        IWebhookJobQueue queue,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookProcessingService> logger)
    {
        _queue = queue;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _templateEngine = new TemplateEngine();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Webhook Processing Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _queue.DequeueAsync(stoppingToken);
                
                // Fire and forget individual job processing to process multiple in parallel?
                // The requirements say "Enterprise Grade", so parallel processing is good.
                // But we don't want to exhaust thread pool. 
                // For now, let's process sequentially or with basic concurrency.
                // Since there is a Delay involved, we shouldn't await the delay in the main loop.
                
                _ = ProcessJobAsync(job, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while dequeuing webhook job.");
            }
        }

        _logger.LogInformation("Webhook Processing Service is stopping.");
    }

    private async Task ProcessJobAsync(WebhookJob job, CancellationToken cancellationToken)
    {
        try
        {
            if (job.Definition.DelayMs > 0)
            {
                await Task.Delay(job.Definition.DelayMs, cancellationToken);
            }

            var context = MapToMockContext(job.Context);
            
            // Render URL
            var url = _templateEngine.Render(job.Definition.Url, context);

            // Render Body
            string? body = null;
            if (!string.IsNullOrEmpty(job.Definition.Body))
            {
                body = _templateEngine.Render(job.Definition.Body, context);
            }

            // Render Headers
            var headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(job.Definition.Headers))
            {
                try
                {
                    // Headers stored as JSON string
                    // We can render the whole JSON string then parse? 
                    // Or parse then render each value?
                    // Render whole string is more flexible.
                    var renderedHeadersJson = _templateEngine.Render(job.Definition.Headers, context);
                    headers = JsonSerializer.Deserialize<Dictionary<string, string>>(renderedHeadersJson) 
                              ?? new Dictionary<string, string>();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse/render webhook headers for Job {RequestId}", job.OriginalRequestId);
                }
            }

            using var client = _httpClientFactory.CreateClient();
            var requestMessage = new HttpRequestMessage(new HttpMethod(job.Definition.Method.ToString()), url);

            if (body != null)
            {
                requestMessage.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            foreach (var header in headers)
            {
                // Try to add without validation first
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value))
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            _logger.LogInformation("Sending Webhook to {Url} (Origin: {RequestId})", url, job.OriginalRequestId);
            
            var response = await client.SendAsync(requestMessage, cancellationToken);
            
            _logger.LogInformation("Webhook to {Url} completed with status {StatusCode}", url, response.StatusCode);

            // Enterprise Grade: Here we would log the detailed result to Database (WebhookLogs table)
            // But that is out of scope for "Basic Implementation" unless requested.
            // Documentation says "Enterprise grade", so maybe I should at least log errors clearly.
            
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Webhook failed. Body: {Body}", responseBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook job for Request {RequestId}", job.OriginalRequestId);
        }
    }

    private static MockRequestContext MapToMockContext(Application.DTOs.Webhook.WebhookExecutionContext dto)
    {
        return new MockRequestContext
        {
            Path = dto.Path,
            Method = dto.Method,
            QueryParams = dto.QueryParams,
            Headers = dto.Headers,
            RouteParams = dto.RouteParams,
            Body = dto.Body,
            RawBody = dto.RawBody
        };
    }
}
