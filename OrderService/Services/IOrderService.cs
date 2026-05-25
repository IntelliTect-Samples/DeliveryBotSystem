using OrderService.DTOs;

namespace OrderService.Services;

public interface IOrderService
{
    Task<OrderResponseDto> PlaceOrderAsync(PlaceOrderDto dto);
    Task<OrderResponseDto?> GetOrderAsync(Guid id);
    Task<IEnumerable<OrderResponseDto>> GetOrderHistoryAsync(string customerId);
}
