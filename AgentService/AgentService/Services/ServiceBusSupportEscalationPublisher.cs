using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using AgentService.Options;

namespace AgentService.Services;

public sealed class ServiceBusSupportEscalationPublisher : ISupportEscalationPublisher
{
    private static readonly TokenRequestContext ServiceBusTokenRequest =
        new(["https://servicebus.azure.net/.default"]);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SupportEscalationOptions _options;
    private readonly DefaultAzureCredential _credential = new();
    private readonly HttpClient _httpClient = new();

    public ServiceBusSupportEscalationPublisher(SupportEscalationOptions options)
    {
        _options = options;
    }

    public async Task PublishAsync(
        SupportEscalationRecord escalation,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured())
        {
            return;
        }

        var token = await _credential.GetTokenAsync(ServiceBusTokenRequest, cancellationToken);
        var namespaceHost = _options.FullyQualifiedNamespace
            .Replace("https://", "", StringComparison.OrdinalIgnoreCase)
            .TrimEnd('/');
        var queueName = Uri.EscapeDataString(_options.QueueName);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri($"https://{namespaceHost}/{queueName}/messages"));

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        request.Headers.Add("BrokerProperties", JsonSerializer.Serialize(new
        {
            Label = "deliverybot-support-escalation",
            CorrelationId = escalation.RelatedOrderId ?? Guid.NewGuid().ToString("N")
        }));
        request.Content = new StringContent(
            JsonSerializer.Serialize(escalation, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Created &&
            response.StatusCode != HttpStatusCode.OK &&
            response.StatusCode != HttpStatusCode.Accepted)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Service Bus returned HTTP {(int)response.StatusCode} while publishing a support escalation: {body}");
        }
    }
}
