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
}
