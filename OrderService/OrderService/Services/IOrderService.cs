// Defines the contract for the Order Service.
using OrderService.DTOs;
using OrderService.Events;

namespace OrderService.Services;

public interface IOrderService
{
    Task<OrderResponseDto> PlaceOrderAsync(PlaceOrderDto dto);
    Task<OrderResponseDto?> GetOrderAsync(Guid id);
    Task<IEnumerable<OrderResponseDto>> GetOrderHistoryAsync(string customerId);

    // Advances order status in response to a bot event from the simulator (#41).
    Task ApplyStatusEventAsync(RobotEventEnvelope evt, CancellationToken ct = default);

    // Asks the AI concierge a free-text question about an order (#43).
    // Returns null when the order doesn't exist.
    Task<string?> AskAboutOrderAsync(Guid id, string question, CancellationToken ct = default);

    // Returns the AI-generated status update history for an order (#43).
    Task<IEnumerable<OrderStatusUpdateDto>> GetOrderUpdatesAsync(Guid id, CancellationToken ct = default);
}
