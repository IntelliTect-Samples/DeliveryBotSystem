using AgentService.Options;
using AgentService.Services;

var builder = WebApplication.CreateBuilder(args);
var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]
    ?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
    ?? ["http://localhost:5173"];

builder.Services.Configure<AzureOpenAiOptions>(
    builder.Configuration.GetSection(AzureOpenAiOptions.SectionName));
builder.Services.Configure<AgentIntegrationOptions>(
    builder.Configuration.GetSection(AgentIntegrationOptions.SectionName));

builder.Services.AddHttpClient<IAgentService, AzureOpenAiAgentService>();

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
