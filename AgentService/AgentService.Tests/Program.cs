using System.Net;
using System.Text.Json;
using AgentService.DTOs;
using AgentService.Options;
using AgentService.Services;
using Microsoft.Extensions.Options;

var tests = new AgentServiceTestRunner();
await tests.RunAsync();

internal sealed class AgentServiceTestRunner
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

    public async Task RunAsync()
    {
        var tests = new List<(string Name, Func<Task> Run)>
        {
            ("BuildUserPrompt includes question and context", () => RunSync(BuildUserPrompt_IncludesQuestionAndContext)),
            ("BuildUserPrompt includes recent conversation history", () => RunSync(BuildUserPrompt_IncludesRecentHistory)),
            ("BuildUserPrompt handles missing history", () => RunSync(BuildUserPrompt_HandlesMissingHistory)),
            ("BuildRequestBody includes recent history and settings", () => RunSync(BuildRequestBody_IncludesHistoryAndSettings)),
            ("ExtractReply reads first choice content", () => RunSync(ExtractReply_ReadsFirstChoiceContent)),
            ("ChatAsync throws when Azure OpenAI is not configured", ChatAsync_ThrowsWhenAzureOpenAiIsNotConfigured),
            ("ChatAsync returns reply and model when Azure OpenAI succeeds", ChatAsync_ReturnsReplyAndModel_WhenAzureOpenAiSucceeds),
            ("ChatAsync throws when Azure OpenAI returns error", ChatAsync_ThrowsWhenAzureOpenAiReturnsError),
            ("ChatAsync posts to Azure OpenAI chat completions endpoint", ChatAsync_PostsToAzureOpenAiChatCompletionsEndpoint)
        };

        foreach (var test in tests)
        {
            await test.Run();
            Console.WriteLine($"PASS {test.Name}");
        }
    }

    private static Task RunSync(Action test)
    {
        test();
        return Task.CompletedTask;
    }

    private static void BuildUserPrompt_IncludesQuestionAndContext()
    {
        var prompt = AzureOpenAiChatMapper.BuildUserPrompt(Request);

        AssertContains(prompt, "What is the ETA?");
        AssertContains(prompt, "bot-002");
        AssertContains(prompt, "9 min");
        AssertContains(prompt, "Spokane Convention Center");
        AssertContains(prompt, "water x1, chips x2");
    }

    private static void BuildUserPrompt_IncludesRecentHistory()
    {
        var prompt = AzureOpenAiChatMapper.BuildUserPrompt(Request);

        AssertContains(prompt, "assistant: I can help with your latest order.");
        AssertContains(prompt, "user: Where is the delivery going?");
    }

    private static void BuildUserPrompt_HandlesMissingHistory()
    {
        var prompt = AzureOpenAiChatMapper.BuildUserPrompt(RequestWithoutHistory);

        AssertContains(prompt, "No earlier conversation is available.");
    }

    private static void BuildRequestBody_IncludesHistoryAndSettings()
    {
        var body = JsonSerializer.Serialize(
            AzureOpenAiChatMapper.BuildRequestBody(Request, MakeOptions()));

        AssertContains(body, "\"temperature\":0.2");
        AssertContains(body, "Where is the delivery going?");
        AssertContains(body, "water x1, chips x2");
    }

    private static void ExtractReply_ReadsFirstChoiceContent()
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

        AssertEqual("The current ETA is about 9 min.", AzureOpenAiChatMapper.ExtractReply(document));
        AssertEqual("gpt-4.1-mini", AzureOpenAiChatMapper.ExtractModel(document));
    }

    private static async Task ChatAsync_ThrowsWhenAzureOpenAiIsNotConfigured()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK), new AzureOpenAiOptions());

        try
        {
            await service.ChatAsync(Request);
            throw new InvalidOperationException("Expected InvalidOperationException.");
        }
        catch (InvalidOperationException error)
        {
            AssertContains(error.Message, "Azure OpenAI is not configured");
        }
    }

    private static async Task ChatAsync_ReturnsReplyAndModel_WhenAzureOpenAiSucceeds()
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

        AssertEqual("The current ETA is about 9 min.", result.Reply);
        AssertEqual("azure-openai", result.Source);
        AssertEqual("gpt-4.1-mini", result.Model);
    }

    private static async Task ChatAsync_ThrowsWhenAzureOpenAiReturnsError()
    {
        var service = CreateService(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":\"bad request\"}")
            },
            MakeOptions());

        try
        {
            await service.ChatAsync(Request);
            throw new InvalidOperationException("Expected InvalidOperationException.");
        }
        catch (InvalidOperationException error)
        {
            AssertContains(error.Message, "HTTP 400");
        }
    }

    private static async Task ChatAsync_PostsToAzureOpenAiChatCompletionsEndpoint()
    {
        Uri? requestedUri = null;

        var service = CreateService(request =>
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

        if (requestedUri is null)
        {
            throw new InvalidOperationException("Expected request URI to be captured.");
        }

        AssertContains(requestedUri.ToString(), "/openai/deployments/delivery-agent/chat/completions");
        AssertContains(requestedUri.ToString(), "api-version=2024-10-21");
    }

    private static AzureOpenAiAgentService CreateService(
        Func<HttpRequestMessage, HttpResponseMessage> respond,
        AzureOpenAiOptions options)
    {
        var httpClient = new HttpClient(new FakeHandler(respond));
        return new AzureOpenAiAgentService(httpClient, Options.Create(options));
    }

    private static AzureOpenAiOptions MakeOptions() => new()
    {
        Endpoint = "https://deliverybot-openai.openai.azure.com",
        Deployment = "delivery-agent",
        ApiKey = "test-key",
        ApiVersion = "2024-10-21"
    };

    private static void AssertContains(string actual, string expected)
    {
        if (!actual.Contains(expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Expected '{actual}' to contain '{expected}'.");
        }
    }

    private static void AssertEqual(string? expected, string? actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected '{expected}' but got '{actual}'.");
        }
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
