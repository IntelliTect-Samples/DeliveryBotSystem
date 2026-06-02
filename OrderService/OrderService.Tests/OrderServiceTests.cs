// Unit tests for Order Service business logic.
// Covers: item mapping by order type, bot assignment status, and geocoding fallback.
// Uses EF Core InMemory + fake HTTP handler — no real database or external APIs needed.
using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Events;
using OrderService.Models;

namespace OrderService.Tests;

public sealed class OrderServiceTests
{
    // ── Setup helpers ──────────────────────────────────────────────────────────

    private static (Services.OrderService svc, OrderDbContext db) CreateService(
        Func<HttpRequestMessage, HttpResponseMessage> httpHandler,
        Dictionary<string, string?> configValues)
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new OrderDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var factory = new FakeHttpClientFactory(new FakeHttpMessageHandler(httpHandler));
        var logger = NullLogger<Services.OrderService>.Instance;

        return (new Services.OrderService(db, factory, config, logger), db);
    }

    private static PlaceOrderDto MakeOrder(string orderType = "Food Order") => new()
    {
        CustomerName = "Jane",
        Phone = "555-1234",
        DeliveryAddress = "123 Main St, Spokane WA",
        OrderType = orderType
    };

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static HttpResponseMessage BotListJson(bool hasAvailableBot) =>
        Json(hasAvailableBot
            ? """[{"id":1,"name":"bot-001","isOnline":true,"isServicingCustomer":false}]"""
            : "[]");

    private static Dictionary<string, string?> Config(string botUrl = "http://fake-bot-api") =>
        new()
        {
            ["BotNetApi:BaseUrl"] = botUrl,
            ["EventHub:ConnectionString"] = "",
            ["EventHub:Name"] = "robot-input"
        };

    // Dispatches to different responses based on whether the request is to Nominatim or BotNetApi
    private static HttpResponseMessage DispatchByUrl(
        HttpRequestMessage req,
        string geocodeJson,
        bool botAvailable)
    {
        return req.RequestUri!.Host.Contains("nominatim")
            ? Json(geocodeJson)
            : BotListJson(botAvailable);
    }

    // ── MapOrderTypeToItems tests (verified through PlaceOrderAsync result) ────

    [Fact]
    public async Task PlaceOrder_FoodOrder_CreatesFoodItem()
    {
        var (svc, _) = CreateService(_ => Json("[]"), Config(botUrl: ""));
        var result = await svc.PlaceOrderAsync(MakeOrder("Food Order"));

        Assert.Contains(result.Items, i => i.ItemId == "food");
    }

    [Fact]
    public async Task PlaceOrder_BeverageOrder_CreatesBeverageItem()
    {
        var (svc, _) = CreateService(_ => Json("[]"), Config(botUrl: ""));
        var result = await svc.PlaceOrderAsync(MakeOrder("Beverage Order"));

        Assert.Contains(result.Items, i => i.ItemId == "beverage");
    }

    [Fact]
    public async Task PlaceOrder_SmallPackage_CreatesPackageItem()
    {
        var (svc, _) = CreateService(_ => Json("[]"), Config(botUrl: ""));
        var result = await svc.PlaceOrderAsync(MakeOrder("Small Package"));

        Assert.Contains(result.Items, i => i.ItemId == "package");
    }

    [Fact]
    public async Task PlaceOrder_UnknownOrderType_DefaultsToFoodItem()
    {
        var (svc, _) = CreateService(_ => Json("[]"), Config(botUrl: ""));
        var result = await svc.PlaceOrderAsync(MakeOrder("Mystery Order"));

        Assert.Contains(result.Items, i => i.ItemId == "food");
    }

    // ── Bot selection / order status tests ────────────────────────────────────

    [Fact]
    public async Task PlaceOrder_AssignedStatus_WhenBotIsAvailable()
    {
        var (svc, _) = CreateService(
            req => DispatchByUrl(req, "[]", botAvailable: true),
            Config());

        var result = await svc.PlaceOrderAsync(MakeOrder());

        Assert.Equal("Assigned", result.Status);
        Assert.Equal("bot-001", result.AssignedBotId);
    }

    [Fact]
    public async Task PlaceOrder_PendingStatus_WhenNoBotsAvailable()
    {
        var (svc, _) = CreateService(
            req => DispatchByUrl(req, "[]", botAvailable: false),
            Config());

        var result = await svc.PlaceOrderAsync(MakeOrder());

        Assert.Equal("Pending", result.Status);
        Assert.Null(result.AssignedBotId);
    }

    [Fact]
    public async Task PlaceOrder_PendingStatus_WhenBotApiNotConfigured()
    {
        var (svc, _) = CreateService(_ => Json("[]"), Config(botUrl: ""));

        var result = await svc.PlaceOrderAsync(MakeOrder());

        Assert.Equal("Pending", result.Status);
        Assert.Null(result.AssignedBotId);
    }

    // ── Geocoding fallback tests ───────────────────────────────────────────────

    [Fact]
    public async Task PlaceOrder_UsesDefaultCoords_WhenGeocodingReturnsEmpty()
    {
        var (svc, _) = CreateService(
            req => DispatchByUrl(req, "[]", botAvailable: false),
            Config(botUrl: ""));

        var result = await svc.PlaceOrderAsync(MakeOrder());

        // Fallback is downtown Spokane
        Assert.Equal(47.6588, result.Destination!.Latitude);
        Assert.Equal(-117.4260, result.Destination!.Longitude);
    }

    [Fact]
    public async Task PlaceOrder_UsesDefaultCoords_WhenGeocodingFails()
    {
        var (svc, _) = CreateService(
            req => req.RequestUri!.Host.Contains("nominatim")
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : BotListJson(false),
            Config(botUrl: ""));

        var result = await svc.PlaceOrderAsync(MakeOrder());

        Assert.Equal(47.6588, result.Destination!.Latitude);
        Assert.Equal(-117.4260, result.Destination!.Longitude);
    }

    [Fact]
    public async Task PlaceOrder_UsesGeocodedCoords_WhenGeocodingSucceeds()
    {
        const string nominatimJson = """[{"lat":"47.6700","lon":"-117.4100"}]""";

        var (svc, _) = CreateService(
            req => DispatchByUrl(req, nominatimJson, botAvailable: false),
            Config(botUrl: ""));

        var result = await svc.PlaceOrderAsync(MakeOrder());

        Assert.Equal(47.6700, result.Destination!.Latitude, precision: 4);
        Assert.Equal(-117.4100, result.Destination!.Longitude, precision: 4);
    }

    // ── Seed / event helpers ──────────────────────────────────────────────────

    private static Order SeedOrder(
        OrderDbContext db,
        string customerId,
        OrderStatus status,
        Guid? id = null,
        DateTime? createdAt = null,
        string itemId = "food")
    {
        var order = new Order
        {
            Id = id ?? Guid.NewGuid(),
            CustomerId = customerId,
            Status = status,
            DeliveryAddress = "123 Main St, Spokane WA",
            DestinationLatitude = 47.6,
            DestinationLongitude = -117.4,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            Items = { new OrderItem { ItemId = itemId, Quantity = 1 } }
        };
        db.Orders.Add(order);
        db.SaveChanges();
        return order;
    }

    private static RobotEventEnvelope Envelope(
        string eventType,
        string? source = "robot-simulator",
        string? orderId = null,
        string? activeOrderId = null,
        string? previousOrderId = null,
        string? result = null,
        string? currentStatus = null,
        string? reason = null) => new()
    {
        EventType = eventType,
        Source = source,
        Data = new RobotEventData
        {
            OrderId = orderId,
            ActiveOrderId = activeOrderId,
            PreviousOrderId = previousOrderId,
            Result = result,
            CurrentStatus = currentStatus,
            Reason = reason
        }
    };

    // ── GetOrderAsync (status by id) — #41 ─────────────────────────────────────

    [Fact]
    public async Task GetOrder_ReturnsNull_WhenOrderDoesNotExist()
    {
        var (svc, _) = CreateService(_ => Json("[]"), Config(botUrl: ""));

        var result = await svc.GetOrderAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetOrder_ReturnsOrderWithStatusAndItems_WhenExists()
    {
        var (svc, db) = CreateService(_ => Json("[]"), Config(botUrl: ""));
        var order = SeedOrder(db, "Jane:555", OrderStatus.InTransit);

        var result = await svc.GetOrderAsync(order.Id);

        Assert.NotNull(result);
        Assert.Equal(order.Id, result!.Id);
        Assert.Equal("InTransit", result.Status);
        Assert.Contains(result.Items, i => i.ItemId == "food");
    }

    // ── GetOrderHistoryAsync — #42 ─────────────────────────────────────────────

    [Fact]
    public async Task GetOrderHistory_ReturnsOnlyMatchingCustomersOrders()
    {
        var (svc, db) = CreateService(_ => Json("[]"), Config(botUrl: ""));
        SeedOrder(db, "Jane:555", OrderStatus.Delivered);
        SeedOrder(db, "Jane:555", OrderStatus.Pending);
        SeedOrder(db, "Bob:999", OrderStatus.Delivered);

        var history = (await svc.GetOrderHistoryAsync("Jane:555")).ToList();

        Assert.Equal(2, history.Count);
        Assert.All(history, o => Assert.Equal("Jane:555", o.CustomerId));
    }

    [Fact]
    public async Task GetOrderHistory_IsOrderedByMostRecentFirst()
    {
        var (svc, db) = CreateService(_ => Json("[]"), Config(botUrl: ""));
        var older = SeedOrder(db, "Jane:555", OrderStatus.Delivered, createdAt: DateTime.UtcNow.AddHours(-2));
        var newer = SeedOrder(db, "Jane:555", OrderStatus.Pending, createdAt: DateTime.UtcNow);

        var history = (await svc.GetOrderHistoryAsync("Jane:555")).ToList();

        Assert.Equal(newer.Id, history[0].Id);
        Assert.Equal(older.Id, history[1].Id);
    }

    [Fact]
    public async Task GetOrderHistory_EachOrderHasItemsDestinationTimestampAndStatus()
    {
        var (svc, db) = CreateService(_ => Json("[]"), Config(botUrl: ""));
        SeedOrder(db, "Jane:555", OrderStatus.Delivered);

        var order = (await svc.GetOrderHistoryAsync("Jane:555")).Single();

        Assert.NotEmpty(order.Items);                 // items ordered
        Assert.NotNull(order.Destination);            // destination
        Assert.NotEqual(default, order.CreatedAt);    // timestamp
        Assert.Equal("Delivered", order.Status);      // final status
    }

    [Fact]
    public async Task GetOrderHistory_ReturnsEmpty_ForUnknownCustomer()
    {
        var (svc, _) = CreateService(_ => Json("[]"), Config(botUrl: ""));

        var history = await svc.GetOrderHistoryAsync("Nobody:000");

        Assert.Empty(history);
    }

    // ── OrderStatusMapping (pure event → status) — #41 ─────────────────────────

    [Theory]
    [InlineData("Accepted", OrderStatus.InTransit)]
    [InlineData("Queued", OrderStatus.Assigned)]
    [InlineData("Rejected", OrderStatus.Failed)]
    public void Map_AssignmentResponse_MapsResultToStatus(string result, OrderStatus expected)
    {
        var orderId = Guid.NewGuid().ToString();
        var change = OrderStatusMapping
            .Map(Envelope("RobotOrderAssignmentResponse", orderId: orderId, result: result))
            .Single();

        Assert.Equal(orderId, change.OrderId);
        Assert.Equal(expected, change.Status);
    }

    [Fact]
    public void Map_StatusUpdated_OnDelivery_MarksActiveOrderInTransit()
    {
        var orderId = Guid.NewGuid().ToString();
        var change = OrderStatusMapping
            .Map(Envelope("RobotStatusUpdated", activeOrderId: orderId,
                currentStatus: "OnDelivery", reason: "OrderAcceptedDeliveryStarted"))
            .Single();

        Assert.Equal(orderId, change.OrderId);
        Assert.Equal(OrderStatus.InTransit, change.Status);
    }

    [Fact]
    public void Map_StatusUpdated_DeliveryCompleted_MarksPreviousOrderDelivered()
    {
        var orderId = Guid.NewGuid().ToString();
        var change = OrderStatusMapping
            .Map(Envelope("RobotStatusUpdated", previousOrderId: orderId,
                currentStatus: "Available", reason: "DeliveryCompletedNoQueuedOrders"))
            .Single();

        Assert.Equal(orderId, change.OrderId);
        Assert.Equal(OrderStatus.Delivered, change.Status);
    }

    [Fact]
    public void Map_DeliveryCompleted_MarksOrderDelivered()
    {
        var orderId = Guid.NewGuid().ToString();
        var change = OrderStatusMapping
            .Map(Envelope("RobotDeliveryCompleted", orderId: orderId))
            .Single();

        Assert.Equal(OrderStatus.Delivered, change.Status);
    }

    [Fact]
    public void Map_UnknownEventType_ProducesNoChanges()
    {
        Assert.Empty(OrderStatusMapping.Map(Envelope("RobotTelemetryUpdated", orderId: "x")));
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Assigned, true)]
    [InlineData(OrderStatus.Assigned, OrderStatus.InTransit, true)]
    [InlineData(OrderStatus.InTransit, OrderStatus.Assigned, false)]
    [InlineData(OrderStatus.Delivered, OrderStatus.InTransit, false)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Failed, false)]
    public void IsForward_OnlyAllowsForwardProgression(OrderStatus current, OrderStatus next, bool expected)
    {
        Assert.Equal(expected, OrderStatusMapping.IsForward(current, next));
    }

    // ── ApplyStatusEventAsync (persisted) — #41 ────────────────────────────────

    [Fact]
    public async Task ApplyStatusEvent_AdvancesPersistedStatus()
    {
        var (svc, db) = CreateService(_ => Json("[]"), Config(botUrl: ""));
        var order = SeedOrder(db, "Jane:555", OrderStatus.Assigned);

        await svc.ApplyStatusEventAsync(
            Envelope("RobotOrderAssignmentResponse", orderId: order.Id.ToString(), result: "Accepted"));

        Assert.Equal(OrderStatus.InTransit, db.Orders.Single().Status);
    }

    [Fact]
    public async Task ApplyStatusEvent_DoesNotRegressTerminalStatus()
    {
        var (svc, db) = CreateService(_ => Json("[]"), Config(botUrl: ""));
        var order = SeedOrder(db, "Jane:555", OrderStatus.Delivered);

        await svc.ApplyStatusEventAsync(
            Envelope("RobotStatusUpdated", activeOrderId: order.Id.ToString(),
                currentStatus: "OnDelivery", reason: "OrderAcceptedDeliveryStarted"));

        Assert.Equal(OrderStatus.Delivered, db.Orders.Single().Status);
    }

    [Fact]
    public async Task ApplyStatusEvent_IgnoresEventsWePublished()
    {
        var (svc, db) = CreateService(_ => Json("[]"), Config(botUrl: ""));
        var order = SeedOrder(db, "Jane:555", OrderStatus.Assigned);

        await svc.ApplyStatusEventAsync(
            Envelope("RobotOrderAssignmentResponse", source: "order-service",
                orderId: order.Id.ToString(), result: "Rejected"));

        Assert.Equal(OrderStatus.Assigned, db.Orders.Single().Status);
    }

    [Fact]
    public async Task ApplyStatusEvent_IgnoresUnknownOrderId()
    {
        var (svc, db) = CreateService(_ => Json("[]"), Config(botUrl: ""));
        SeedOrder(db, "Jane:555", OrderStatus.Assigned);

        // Should not throw and should not touch the existing order.
        await svc.ApplyStatusEventAsync(
            Envelope("RobotDeliveryCompleted", orderId: Guid.NewGuid().ToString()));

        Assert.Equal(OrderStatus.Assigned, db.Orders.Single().Status);
    }

    // ── Fakes ─────────────────────────────────────────────────────────────────

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(respond(request));
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(handler, disposeHandler: false);
    }
}
