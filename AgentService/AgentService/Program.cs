using AgentService.Options;
using AgentService.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]
    ?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
    ?? ["http://localhost:5173"];

builder.Services.Configure<AzureOpenAiOptions>(
    builder.Configuration.GetSection(AzureOpenAiOptions.SectionName));
builder.Services.Configure<AgentIntegrationOptions>(
    builder.Configuration.GetSection(AgentIntegrationOptions.SectionName));

builder.Services.Configure<KeyVaultOptions>(
    builder.Configuration.GetSection(KeyVaultOptions.SectionName));

builder.Services.Configure<TranscriptArchiveOptions>(
    builder.Configuration.GetSection(TranscriptArchiveOptions.SectionName));
builder.Services.Configure<AzureAiSearchOptions>(
    builder.Configuration.GetSection(AzureAiSearchOptions.SectionName));
builder.Services.Configure<SupportEscalationOptions>(
    builder.Configuration.GetSection(SupportEscalationOptions.SectionName));

builder.Services.AddHttpClient<IAgentService, AzureOpenAiAgentService>();

builder.Services.AddSingleton<IAzureOpenAiApiKeyProvider, AzureOpenAiApiKeyProvider>();
builder.Services.AddSingleton<IAgentGroundingService>(sp =>
{
    var options = sp.GetRequiredService<IOptions<AzureAiSearchOptions>>().Value;
    if (options.IsConfigured())
    {
        return new AzureAiSearchGroundingService(options);
    }

    return new NoOpAgentGroundingService();
});

builder.Services.AddSingleton<IChatTranscriptArchive>(sp =>
{
    var options = sp.GetRequiredService<IOptions<TranscriptArchiveOptions>>().Value;
    if (options.IsConfigured())
    {
        return new BlobChatTranscriptArchive(options);
    }

    return new NoOpChatTranscriptArchive();
});
builder.Services.AddSingleton<ISupportEscalationPublisher>(sp =>
{
    var options = sp.GetRequiredService<IOptions<SupportEscalationOptions>>().Value;
    if (options.IsConfigured())
    {
        return new ServiceBusSupportEscalationPublisher(options);
    }

    return new NoOpSupportEscalationPublisher();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CustomerFrontend", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("CustomerFrontend");
app.MapControllers();

app.Run();
