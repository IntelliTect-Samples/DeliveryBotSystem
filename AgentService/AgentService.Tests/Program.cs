using System.Net;
using System.Net.Http;
using System.Text.Json;
using AgentService.DTOs;
using AgentService.Options;
using AgentService.Services;
using Azure.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AgentService.Tests;

public sealed class AzureOpenAiAgentServiceTests
{
    private static readonly AgentChatRequestDto Request = new()
    {
        Message = "What is the ETA?",
        Context = new AgentChatContextDto
        {
            LatestOrder = new AgentLatestOrderDto
            {
                Id = "mock-178",
                Status = "Assigned",
                AssignedBotId = "bot-002",
                DeliveryAddress = "Spokane Convention Center",
                ItemsSummary = "water x1, chips x2"
            },
            Route = new AgentRouteDto
            {
                Distance = "1.8 km",
                Eta = "9 min",
                Source = "osrm"
            }
        },
        History =
        [
            new AgentChatMessageDto
            {
                Role = "assistant",
                Text = "I can help with your latest order."
            },
            new AgentChatMessageDto
            {
                Role = "user",
                Text = "Where is the delivery going?"
            }
        ]
    };

    private static readonly AgentChatRequestDto RequestWithoutHistory = new()
    {
        Message = "What is the ETA?",
        Context = new AgentChatContextDto
        {
            LatestOrder = new AgentLatestOrderDto
            {
                Id = "mock-178",
                Status = "Assigned",
                AssignedBotId = "bot-002",
                DeliveryAddress = "Spokane Convention Center",
                ItemsSummary = "water x1, chips x2"
            },
            Route = new AgentRouteDto
            {
                Distance = "1.8 km",
                Eta = "9 min",
                Source = "osrm"
            }
        }
    };

    [Fact]
    public void BuildUserPrompt_IncludesQuestionAndContext()
    {
        var prompt = AzureOpenAiChatMapper.BuildUserPrompt(Request);

        Assert.Contains("What is the ETA?", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bot-002", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("9 min", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Spokane Convention Center", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("water x1, chips x2", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildUserPrompt_IncludesRecentHistory()
    {
        var prompt = AzureOpenAiChatMapper.BuildUserPrompt(Request);

        Assert.Contains("assistant: I can help with your latest order.", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("user: Where is the delivery going?", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildUserPrompt_HandlesMissingHistory()
    {
        var prompt = AzureOpenAiChatMapper.BuildUserPrompt(RequestWithoutHistory);

        Assert.Contains("No earlier conversation is available.", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildUserPrompt_IncludesLiveServiceNotes()
    {
        var request = new AgentChatRequestDto
        {
            Message = Request.Message,
            Context = new AgentChatContextDto
            {
                LatestOrder = Request.Context?.LatestOrder,
                Route = Request.Context?.Route,
                LiveDataSummary = "- Live order status: InTransit"
            },
            History = Request.History
        };

        var prompt = AzureOpenAiChatMapper.BuildUserPrompt(request);

        Assert.Contains("Live service data:", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Live order status: InTransit", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildUserPrompt_IncludesGroundingSummary()
    {
        var request = new AgentChatRequestDto
        {
            Message = Request.Message,
            Context = new AgentChatContextDto
            {
                LatestOrder = Request.Context?.LatestOrder,
                Route = Request.Context?.Route,
                GroundingSummary = "- [1] Late Delivery Escalation (Support escalation policy): Send late deliveries to support."
            },
            History = Request.History
        };

        var prompt = AzureOpenAiChatMapper.BuildUserPrompt(request);

        Assert.Contains("Knowledge base grounding:", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Late Delivery Escalation", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildRequestBody_IncludesHistoryAndSettings()
    {
        var body = JsonSerializer.Serialize(
            AzureOpenAiChatMapper.BuildRequestBody(Request, MakeOptions()));

        Assert.Contains("\"temperature\":0.2", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Where is the delivery going?", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("water x1, chips x2", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExtractReply_ReadsFirstChoiceContent()
    {
        using var document = JsonDocument.Parse("""
        {
          "model": "gpt-4.1-mini",
          "choices": [
            {
              "message": {
                "content": "The current ETA is about 9 min."
              }
            }
          ]
        }
        """);

        Assert.Equal("The current ETA is about 9 min.", AzureOpenAiChatMapper.ExtractReply(document));
        Assert.Equal("gpt-4.1-mini", AzureOpenAiChatMapper.ExtractModel(document));
    }

    [Fact]
    public async Task ChatAsync_ThrowsWhenAzureOpenAiIsNotConfigured()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK), new AzureOpenAiOptions());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ChatAsync(Request));

        Assert.Contains("Azure OpenAI is not configured", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChatAsync_UsesApiKeyHeader_WhenApiKeyConfigured()
    {
        string? headerValue = null;

        var service = CreateService(
            request =>
            {
                request.Headers.TryGetValues("api-key", out var values);
                headerValue = values?.SingleOrDefault();

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                    {
                      "choices": [
                        {
                          "message": {
                            "content": "The current ETA is about 9 min."
                          }
                        }
                      ]
                    }
                    """)
                };
            },
            MakeOptions());

        await service.ChatAsync(Request);

        Assert.Equal("test-key", headerValue);
    }

    [Fact]
    public async Task ChatAsync_ArchivesSuccessfulResponses()
    {
        var archive = new RecordingChatTranscriptArchive();
        var service = CreateService(_ => JsonResponse("""
                {
                  "choices": [
                    {
                      "message": {
                        "content": "The current ETA is about 9 min."
                      }
                    }
                  ]
                }
                """),
            MakeOptions(),
            archive: archive);

        await service.ChatAsync(Request);

        Assert.Single(archive.Records);
        Assert.Equal("mock-178", archive.Records[0].RelatedOrderId);
    }

    [Fact]
    public async Task ChatAsync_PublishesEscalationForSupportRequest()
    {
        var publisher = new RecordingSupportEscalationPublisher();
        var request = new AgentChatRequestDto
        {
            Message = "This delivery is late. I need support.",
            Context = Request.Context
        };
        var service = CreateService(_ => JsonResponse("""
                {
                  "choices": [
                    {
                      "message": {
                        "content": "I am sending this to support."
                      }
                    }
                  ]
                }
                """),
            MakeOptions(),
            supportEscalationPublisher: publisher);

        await service.ChatAsync(request);

        Assert.Single(publisher.Records);
        Assert.Equal("customer-request", publisher.Records[0].Reason);
    }

    [Fact]
    public async Task ChatAsync_ContinuesWhenArchiveAndEscalationFail()
    {
        var service = CreateService(_ => JsonResponse("""
                {
                  "choices": [
                    {
                      "message": {
                        "content": "I am sending this to support."
                      }
                    }
                  ]
                }
                """),
            MakeOptions(),
            archive: new ThrowingChatTranscriptArchive(),
            supportEscalationPublisher: new ThrowingSupportEscalationPublisher());

        var result = await service.ChatAsync(new AgentChatRequestDto
        {
            Message = "My delivery is late",
            Context = Request.Context
        });

        Assert.Equal("I am sending this to support.", result.Reply);
    }

    [Fact]
    public async Task ChatAsync_EnrichesRequestWithLiveOrderAndBotData()
    {
        string? openAiRequestBody = null;
        var request = new AgentChatRequestDto
        {
            Message = "What is my order status?",
            Context = new AgentChatContextDto
            {
                LatestOrder = new AgentLatestOrderDto
                {
                    Id = "11111111-1111-1111-1111-111111111111"
                }
            }
        };

        var service = CreateService(
            httpRequest =>
            {
                var path = httpRequest.RequestUri?.AbsolutePath ?? "";

                if (path.Contains("/api/orders/", StringComparison.OrdinalIgnoreCase))
                {
                    return JsonResponse("""
                    {
                      "id": "11111111-1111-1111-1111-111111111111",
                      "customerId": "customer-1",
                      "assignedBotId": "bot-007",
                      "status": "InTransit",
                      "deliveryAddress": "123 Riverfront Ave",
                      "items": [
                        { "itemId": "water", "quantity": 1 },
                        { "itemId": "chips", "quantity": 2 }
                      ]
                    }
                    """);
                }

                if (path.EndsWith("/bots/bot-007", StringComparison.OrdinalIgnoreCase))
                {
                    return JsonResponse("""
                    {
                      "botId": "bot-007",
                      "status": "OnDelivery",
                      "powerLevel": 83.6,
                      "queuedOrderCount": 1,
                      "activeOrderId": "11111111-1111-1111-1111-111111111111",
                      "currentLocation": {
                        "latitude": 47.661,
                        "longitude": -117.42
                      }
                    }
                    """);
                }

                openAiRequestBody = httpRequest.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
                return JsonResponse("""
                {
                  "model": "gpt-4.1-mini",
                  "choices": [
                    {
                      "message": {
                        "content": "Your order is on the way."
                      }
                    }
                  ]
                }
                """);
            },
            MakeOptions(),
            new AgentIntegrationOptions
            {
                OrderServiceBaseUrl = "https://orders.example.test",
                SimulatorBaseUrl = "https://simulator.example.test"
            });

        await service.ChatAsync(request);

        Assert.Equal("InTransit", request.Context?.LatestOrder?.Status);
        Assert.Equal("bot-007", request.Context?.LatestOrder?.AssignedBotId);
        Assert.Equal("123 Riverfront Ave", request.Context?.LatestOrder?.DeliveryAddress);
        Assert.Equal("water x1, chips x2", request.Context?.LatestOrder?.ItemsSummary);
        Assert.Contains("Live order status: InTransit", request.Context?.LiveDataSummary ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Live bot status: OnDelivery", request.Context?.LiveDataSummary ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Live bot battery: 84%", request.Context?.LiveDataSummary ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Live bot queued orders: 1", openAiRequestBody ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChatAsync_ToleratesLiveServiceLookupFailures()
    {
        var request = new AgentChatRequestDto
        {
            Message = "Summarize my delivery",
            Context = new AgentChatContextDto
            {
                LatestOrder = new AgentLatestOrderDto
                {
                    Id = "22222222-2222-2222-2222-222222222222",
                    Status = "Assigned"
                }
            }
        };

        var service = CreateService(
            httpRequest =>
            {
                var path = httpRequest.RequestUri?.AbsolutePath ?? "";

                if (path.Contains("/api/orders/", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent("{\"error\":\"down\"}")
                    };
                }

                return JsonResponse("""
                {
                  "choices": [
                    {
                      "message": {
                        "content": "I only have limited live data right now."
                      }
                    }
                  ]
                }
                """);
            },
            MakeOptions(),
            new AgentIntegrationOptions
            {
                OrderServiceBaseUrl = "https://orders.example.test",
                SimulatorBaseUrl = "https://simulator.example.test"
            });

        var result = await service.ChatAsync(request);

        Assert.Equal("I only have limited live data right now.", result.Reply);
        Assert.Equal("Assigned", request.Context?.LatestOrder?.Status);
        Assert.Null(request.Context?.LiveDataSummary);
    }

    [Fact]
    public async Task ChatAsync_ReturnsReplyAndModel_WhenAzureOpenAiSucceeds()
    {
        var service = CreateService(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                  "model": "gpt-4.1-mini",
                  "choices": [
                    {
                      "message": {
                        "content": "The current ETA is about 9 min."
                      }
                    }
                  ]
                }
                """)
            },
            MakeOptions());

        var result = await service.ChatAsync(Request);

        Assert.Equal("The current ETA is about 9 min.", result.Reply);
        Assert.Equal("azure-openai", result.Source);
        Assert.Equal("gpt-4.1-mini", result.Model);
    }

    [Fact]
    public async Task ChatAsync_ThrowsWhenAzureOpenAiReturnsError()
    {
        var service = CreateService(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":\"bad request\"}")
            },
            MakeOptions());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ChatAsync(Request));

        Assert.Contains("HTTP 400", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChatAsync_PostsToAzureOpenAiChatCompletionsEndpoint()
    {
        Uri? requestedUri = null;

        var service = CreateService(
            request =>
            {
                requestedUri = request.RequestUri;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                    {
                      "choices": [
                        {
                          "message": {
                            "content": "ready"
                          }
                        }
                      ]
                    }
                    """)
                };
            },
            MakeOptions());

        await service.ChatAsync(Request);

        Assert.NotNull(requestedUri);
        Assert.Contains("/openai/deployments/delivery-agent/chat/completions", requestedUri!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("api-version=2024-10-21", requestedUri.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static AzureOpenAiAgentService CreateService(
        Func<HttpRequestMessage, HttpResponseMessage> respond,
        AzureOpenAiOptions options)
    {
        return CreateService(respond, options, new AgentIntegrationOptions());
    }

    private static AzureOpenAiAgentService CreateService(
        Func<HttpRequestMessage, HttpResponseMessage> respond,
        AzureOpenAiOptions options,
        AgentIntegrationOptions? integrationOptions = null,
        IAgentGroundingService? groundingService = null,
        IChatTranscriptArchive? archive = null,
        ISupportEscalationPublisher? supportEscalationPublisher = null)
    {
        var httpClient = new HttpClient(new FakeHandler(respond));
        return new AzureOpenAiAgentService(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(options),
            Microsoft.Extensions.Options.Options.Create(integrationOptions ?? new AgentIntegrationOptions()),
            new StaticAzureOpenAiApiKeyProvider(options.ApiKey),
            groundingService ?? new NoOpAgentGroundingService(),
            archive ?? new NoOpChatTranscriptArchive(),
            supportEscalationPublisher ?? new NoOpSupportEscalationPublisher(),
            new StaticTokenCredential(),
            NullLogger<AzureOpenAiAgentService>.Instance);
    }

    private static AzureOpenAiOptions MakeOptions() => new()
    {
        Endpoint = "https://deliverybot-openai.openai.azure.com",
        Deployment = "delivery-agent",
        ApiKey = "test-key",
        ApiVersion = "2024-10-21"
    };

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }

    private sealed class StaticAzureOpenAiApiKeyProvider(string? apiKey) : IAzureOpenAiApiKeyProvider
    {
        public Task<string?> GetApiKeyAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(apiKey);
    }

    private sealed class StaticTokenCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new("test-token", DateTimeOffset.UtcNow.AddMinutes(30));

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            ValueTask.FromResult(GetToken(requestContext, cancellationToken));
    }

    private sealed class RecordingChatTranscriptArchive : IChatTranscriptArchive
    {
        public List<AgentChatTranscriptRecord> Records { get; } = [];

        public Task ArchiveAsync(AgentChatTranscriptRecord transcript, CancellationToken cancellationToken = default)
        {
            Records.Add(transcript);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingChatTranscriptArchive : IChatTranscriptArchive
    {
        public Task ArchiveAsync(AgentChatTranscriptRecord transcript, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("archive failed");
    }

    private sealed class RecordingSupportEscalationPublisher : ISupportEscalationPublisher
    {
        public List<SupportEscalationRecord> Records { get; } = [];

        public Task PublishAsync(SupportEscalationRecord escalation, CancellationToken cancellationToken = default)
        {
            Records.Add(escalation);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingSupportEscalationPublisher : ISupportEscalationPublisher
    {
        public Task PublishAsync(SupportEscalationRecord escalation, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("publish failed");
    }
}
