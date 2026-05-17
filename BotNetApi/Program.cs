using BotNetApi.Data;
using BotNetApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database ───────────────────────────────────────────────────────────────────
// Swap the connection string in appsettings.json to point at Azure SQL for production.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Services ───────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IBotService, BotService>();

// ── Controllers ────────────────────────────────────────────────────────────────
// JsonStringEnumConverter lets clients send enum values as strings ("High", "Medium", "Low")
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// ── Swagger / OpenAPI ──────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ── Development middleware ─────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    // Auto-apply migrations and seed data on startup (dev convenience only)
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database migration failed on startup. Ensure LocalDB is installed and the connection string is correct.");
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
