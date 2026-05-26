// Application startup — wires together the database, services, HTTP clients, and Swagger.
// Runs EF Core migrations automatically on startup so tables are always up to date.
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ───────────────────────────────────────────────────────────────────
// Uses Managed Identity in Azure — no password needed
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Services ───────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IOrderService, OrderService.Services.OrderService>();
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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
