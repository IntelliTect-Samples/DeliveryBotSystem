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
