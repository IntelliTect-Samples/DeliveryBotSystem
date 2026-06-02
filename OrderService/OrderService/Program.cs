// Application startup — wires together the database, services, HTTP clients, and Swagger.
// Runs EF Core migrations automatically on startup so tables are always up to date.
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Events;
using OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ───────────────────────────────────────────────────────────────────
// Uses Managed Identity in Azure — no password needed
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Services ───────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IOrderService, OrderService.Services.OrderService>();
// Consumes bot events from Event Hub and advances order status (#41).
builder.Services.AddHostedService<OrderStatusConsumer>();
builder.Services.AddHttpClient();
// Nominatim requires a User-Agent header or it rejects requests
builder.Services.AddHttpClient("Nominatim", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("DeliveryBotSystem/1.0");
});

// ── Controllers ────────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// ── Swagger / OpenAPI ──────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── CORS ─────────────────────────────────────────────────────────────────────
// Allow the Admin & Maintenance App (issue #18, Orders view #53) to call this
// API from the browser.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AdminApp", policy =>
        policy.WithOrigins(
                  "https://wa-deliverybot-admin-dev.azurewebsites.net", // deployed admin app
                  "http://localhost:5173")                              // local Vite dev server
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// ── Auto-migrate on startup ────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database migration failed on startup.");
    }
}

// ── Middleware ─────────────────────────────────────────────────────────────────
// Swagger UI is enabled in all environments so the deployed API can be explored
// from the browser at /swagger.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AdminApp");
app.MapControllers();

app.Run();
