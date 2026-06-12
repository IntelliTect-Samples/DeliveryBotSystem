using AgentService.Options;
using AgentService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AzureOpenAiOptions>(
    builder.Configuration.GetSection(AzureOpenAiOptions.SectionName));

builder.Services.AddHttpClient<IAgentService, AzureOpenAiAgentService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CustomerFrontend", policy =>
        policy.WithOrigins(
                "https://wa-deliverybot-dev.azurewebsites.net",
                "https://wa-deliverybot-final.azurewebsites.net",
                "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("CustomerFrontend");
app.MapControllers();

app.Run();
