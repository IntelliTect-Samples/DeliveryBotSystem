// Core logic for the Order Service.
// Handles: geocoding delivery addresses, selecting an available bot from BotNetApi,
// saving orders to the database, and publishing order assignment events to Azure Event Hub.
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Events;
using OrderService.Models;

namespace OrderService.Services;

public class OrderService : IOrderService
{
    private readonly OrderDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        OrderDbContext db,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<OrderService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<OrderResponseDto> PlaceOrderAsync(PlaceOrderDto dto)
    {
        // 1. Geocode the delivery address to GPS coordinates
        var (latitude, longitude) = await GeocodeAddressAsync(dto.DeliveryAddress);

        // 2. Pick an available bot from BotNetApi
        var botId = await SelectBotAsync();

        // 3. Map the form's order type to item IDs the simulator understands
        var items = MapOrderTypeToItems(dto.OrderType);

        // 4. Build and save the order
        var customerId = $"{dto.CustomerName}:{dto.Phone}";
        var order = new Order
        {
            CustomerId = customerId,
            AssignedBotId = botId,
            DeliveryAddress = dto.DeliveryAddress,
            DestinationLatitude = latitude,
            DestinationLongitude = longitude,
            Status = botId is not null ? OrderStatus.Assigned : OrderStatus.Pending,
            Items = items.Select(i => new OrderItem
            {
                ItemId = i.ItemId,
                Quantity = i.Quantity
            }).ToList()
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // 5. Publish RobotOrderAssignment event to Event Hub
        if (botId is not null)
            await PublishOrderAssignmentAsync(order, botId);

        _logger.LogInformation(
            "Order placed. OrderId={OrderId} CustomerId={CustomerId} BotId={BotId} Address={Address}",
            order.Id, order.CustomerId, order.AssignedBotId, order.DeliveryAddress);

        return ToResponseDto(order);
    }

    public async Task<OrderResponseDto?> GetOrderAsync(Guid id)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        return order is null ? null : ToResponseDto(order);
    }

    public async Task<IEnumerable<OrderResponseDto>> GetOrderHistoryAsync(string customerId)
    {
        var orders = await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(ToResponseDto);
    }

    // Returns every order, newest first. Backs the admin Orders view (issue #53).
    public async Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync()
    {
        var orders = await _db.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(ToResponseDto);
    }

    public async Task ApplyStatusEventAsync(RobotEventEnvelope evt, CancellationToken ct = default)
    {
        // Never react to events we published ourselves (RobotOrderAssignment).
        if (string.Equals(evt.Source, "order-service", StringComparison.OrdinalIgnoreCase))
            return;

        foreach (var change in OrderStatusMapping.Map(evt))
        {
            if (!Guid.TryParse(change.OrderId, out var orderId))
                continue;

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (order is null)
                continue; // event for an order we don't own (or not yet persisted)

            // Forward-only: ignore duplicate/out-of-order events that would regress status.
            if (!OrderStatusMapping.IsForward(order.Status, change.Status))
                continue;

            var previous = order.Status;
            order.Status = change.Status;
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Order status updated from bot event. OrderId={OrderId} {From}->{To} EventType={EventType}",
                order.Id, previous, change.Status, evt.EventType);
        }
    }

    // Calls OpenStreetMap Nominatim to convert a text address to GPS coordinates.
    // Falls back to downtown Spokane if geocoding fails so orders still go through.
    private async Task<(double Latitude, double Longitude)> GeocodeAddressAsync(string address)
    {
        const double defaultLat = 47.6588;
        const double defaultLon = -117.4260;

        try
        {
            var client = _httpClientFactory.CreateClient("Nominatim");
            var encoded = Uri.EscapeDataString(address);
            var response = await client.GetAsync(
                $"https://nominatim.openstreetmap.org/search?q={encoded}&format=json&limit=1");

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<List<NominatimResult>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (results is null || results.Count == 0)
            {
                _logger.LogWarning("Geocoding returned no results for address: {Address}. Using default location.", address);
                return (defaultLat, defaultLon);
            }

            return (double.Parse(results[0].Lat), double.Parse(results[0].Lon));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Geocoding failed for address: {Address}. Using default location.", address);
            return (defaultLat, defaultLon);
        }
    }

    // Order options mirror the simulator's bot stock catalog (RobotSimulator BotFleet):
    // water, soda, chips, sandwich. Bots reject anything they don't stock, so the
    // Order Service follows the simulator's catalog directly rather than mapping
    // abstract order types. Accepts the item id ("water") or display name ("Water").
    private static List<(string ItemId, int Quantity)> MapOrderTypeToItems(string orderType) =>
        orderType?.Trim().ToLowerInvariant() switch
        {
            "soda"     => [("soda", 1)],
            "chips"    => [("chips", 1)],
            "sandwich" => [("sandwich", 1)],
            _          => [("water", 1)]   // "water" and any unrecognized value → water (always stocked)
        };

    // Calls BotNetApi and returns the Name of the first available bot
    private async Task<string?> SelectBotAsync()
    {
        var botApiUrl = _config["BotNetApi:BaseUrl"];
        if (string.IsNullOrWhiteSpace(botApiUrl))
        {
            _logger.LogWarning("BotNetApi:BaseUrl is not configured. Skipping bot selection.");
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{botApiUrl}/api/bots");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var bots = JsonSerializer.Deserialize<List<BotDto>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Pick the first bot that is online and not currently servicing a customer
            var available = bots?.FirstOrDefault(b => b.IsOnline && !b.IsServicingCustomer);
            // Use Name as the bot ID — simulator tracks bots by name (e.g. "bot-001")
            return available?.Name;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to contact BotNetApi for bot selection. Order will be Pending.");
            return null;
        }
    }

    // Publishes a RobotOrderAssignment event to Azure Event Hub
    private async Task PublishOrderAssignmentAsync(Order order, string botId)
    {
        var connectionString = _config["EventHub:ConnectionString"];
        var eventHubName = _config["EventHub:Name"];

        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(eventHubName))
        {
            _logger.LogWarning("EventHub is not configured. Skipping event publish.");
            return;
        }

        try
        {
            var producer = new Azure.Messaging.EventHubs.Producer.EventHubProducerClient(
                connectionString, eventHubName);

            await using (producer)
            {
                var payload = new
                {
                    eventId = Guid.NewGuid().ToString("N"),
                    eventType = "RobotOrderAssignment",
                    schemaVersion = "1.0",
                    timestampUtc = DateTimeOffset.UtcNow,
                    botId,
                    source = "order-service",
                    isSimulated = false,
                    data = new
                    {
                        orderId = order.Id.ToString(),
                        botId,
                        items = order.Items.Select(i => new { itemId = i.ItemId, quantity = i.Quantity }),
                        destination = new
                        {
                            latitude = order.DestinationLatitude,
                            longitude = order.DestinationLongitude
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var eventData = new Azure.Messaging.EventHubs.EventData(Encoding.UTF8.GetBytes(json));
                await producer.SendAsync([eventData]);

                _logger.LogInformation(
                    "Published RobotOrderAssignment event. OrderId={OrderId} BotId={BotId}",
                    order.Id, botId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish order assignment event. OrderId={OrderId}", order.Id);
        }
    }

    private static OrderResponseDto ToResponseDto(Order order) => new()
    {
        Id = order.Id,
        CustomerId = order.CustomerId,
        AssignedBotId = order.AssignedBotId,
        Status = order.Status.ToString(),
        DeliveryAddress = order.DeliveryAddress,
        Destination = new GpsLocationDto
        {
            Latitude = order.DestinationLatitude,
            Longitude = order.DestinationLongitude
        },
        Items = order.Items.Select(i => new OrderItemDto
        {
            ItemId = i.ItemId,
            Quantity = i.Quantity
        }).ToList(),
        CreatedAt = order.CreatedAt
    };

    // Minimal shape of what BotNetApi returns — only fields we need
    private sealed class BotDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public bool IsServicingCustomer { get; set; }
    }

    // Nominatim geocoding response shape
    private sealed class NominatimResult
    {
        public string Lat { get; set; } = string.Empty;
        public string Lon { get; set; } = string.Empty;
    }
}
