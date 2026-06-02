using DeliveryBot.RobotSimulator.Api;
using DeliveryBot.RobotSimulator.Core.Bots;
using DeliveryBot.RobotSimulator.Core.Orders;
using DeliveryBot.RobotSimulator.Core.Simulation;
using DeliveryBot.RobotSimulator.Events;
using DeliveryBot.RobotSimulator.Infrastructure.Events;
using DeliveryBot.RobotSimulator.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

var simulatorOptions = builder.Configuration
    .GetSection("Simulator")
    .Get<SimulatorOptions>() ?? new SimulatorOptions();

var simulationOptions = builder.Configuration
    .GetSection("Simulation")
    .Get<SimulationOptions>() ?? new SimulationOptions();

builder.Services.AddSingleton(simulatorOptions);
builder.Services.AddSingleton(simulationOptions);

builder.Services.AddSingleton<BotFleet>();
builder.Services.AddSingleton<OrderAssignmentHandler>();

builder.Services.AddRobotEventTransport(builder.Configuration);

builder.Services.AddHostedService<SimulationWorker>();
builder.Services.AddHostedService<EventHubOrderAssignmentWorker>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Allow the Admin & Maintenance App (issue #18) to call the simulator from the browser.
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

var fleet = app.Services.GetRequiredService<BotFleet>();
var startupOptions = app.Services.GetRequiredService<SimulatorOptions>();

fleet.InitializeDefaultFleet(startupOptions);

app.Logger.LogInformation(
    "Initialized robot simulator fleet. InitialBotCount={InitialBotCount} BotIdPrefix={BotIdPrefix} DefaultLocation=({Latitude}, {Longitude})",
    startupOptions.InitialBotCount,
    startupOptions.BotIdPrefix,
    startupOptions.DefaultLatitude,
    startupOptions.DefaultLongitude);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AdminApp");

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "robot-simulator"
}));

app.MapGet("/bots", (BotFleet botFleet) =>
{
    return Results.Ok(botFleet.GetAll());
});

app.MapGet("/bots/{botId}", (string botId, BotFleet botFleet) =>
{
    var bot = botFleet.Get(botId);

    return bot is null
        ? Results.NotFound(new { message = $"Bot {botId} was not found." })
        : Results.Ok(bot);
});

app.MapPost("/bots", async (
    CreateBotRequest request,
    BotFleet botFleet,
    SimulatorOptions simulatorOptions,
    IRobotEventPublisher eventPublisher,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.BotId))
    {
        return Results.BadRequest(new
        {
            message = "BotId is required."
        });
    }

    var model = string.IsNullOrWhiteSpace(request.Model)
        ? simulatorOptions.DefaultBotModel
        : request.Model;

    var location = request.CurrentLocation
        ?? new GeoLocation(
            simulatorOptions.DefaultLatitude,
            simulatorOptions.DefaultLongitude);

    SimulatedBot bot;

    try
    {
        bot = botFleet.AddBot(
            request.BotId,
            model,
            location);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new
        {
            message = ex.Message
        });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new
        {
            message = ex.Message
        });
    }

    var snapshot = bot.ToSnapshot();

    var createdEvent = RobotEventEnvelope.Create(
        RobotEventTypes.BotCreated,
        new
        {
            bot = snapshot
        },
        snapshot.BotId);

    await eventPublisher.PublishAsync(
        createdEvent,
        cancellationToken);

    return Results.Created($"/bots/{snapshot.BotId}", snapshot);
});

app.MapPatch("/bots/{botId}", async (
    string botId,
    UpdateBotRequest request,
    BotFleet botFleet,
    IRobotEventPublisher eventPublisher,
    CancellationToken cancellationToken) =>
{
    var updatedBot = botFleet.UpdateBot(botId, request);

    if (updatedBot is null)
    {
        return Results.NotFound(new
        {
            message = $"Bot {botId} was not found."
        });
    }

    var updatedEvent = RobotEventEnvelope.Create(
        RobotEventTypes.BotUpdated,
        new
        {
            bot = updatedBot
        },
        updatedBot.BotId);

    await eventPublisher.PublishAsync(
        updatedEvent,
        cancellationToken);

    return Results.Ok(updatedBot);
});

app.MapDelete("/bots/{botId}", async (
    string botId,
    BotFleet botFleet,
    IRobotEventPublisher eventPublisher,
    CancellationToken cancellationToken) =>
{
    var removed = botFleet.RemoveBot(
        botId,
        out var removedBot,
        out var reason);

    if (!removed)
    {
        if (reason == "BotNotFound")
        {
            return Results.NotFound(new
            {
                message = $"Bot {botId} was not found."
            });
        }

        return Results.Conflict(new
        {
            message = $"Bot {botId} cannot be removed because it has active or queued orders.",
            reason
        });
    }

    var removedEvent = RobotEventEnvelope.Create(
        RobotEventTypes.BotRemoved,
        new
        {
            bot = removedBot
        },
        removedBot!.BotId);

    await eventPublisher.PublishAsync(
        removedEvent,
        cancellationToken);

    return Results.NoContent();
});

app.MapPost("/orders/assignments", async (
    OrderAssignment assignment,
    OrderAssignmentHandler handler,
    BotFleet botFleet,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(
        assignment,
        cancellationToken);

    if (!botFleet.TryGetBot(assignment.BotId, out _))
    {
        return Results.NotFound(result);
    }

    return Results.Ok(result);
});

app.MapGet("/events/recent", (
    RecentRobotEventStore eventStore,
    int count = 50) =>
{
    count = Math.Clamp(count, 1, 100);

    return Results.Ok(eventStore.GetRecent(count));
});

app.Run();