using BotNetApi.Data;
using BotNetApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database ───────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Services ───────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IBotService, BotService>();

// ── Controllers ────────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// ── Swagger / OpenAPI ──────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "BotNetApi",
        Version = "v1",
        Description = "Backend API for the vending machine bot delivery network. " +
                      "Manages bot status, battery levels, and availability."
    });
});

// ── CORS ─────────────────────────────────────────────────────────────────────
// Allow the Admin & Maintenance App (issue #18) to call this API from the browser.
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

// ── Apply EF Core migrations on startup ───────────────────────────────────────
// Runs in all environments so the container auto-migrates on first start.
// EF Core tracks applied migrations in __EFMigrationsHistory, making this idempotent.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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

// ── Development middleware ─────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BotNetApi v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AdminApp");
app.MapControllers();

app.Run();
