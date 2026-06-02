// Defines the contract for the Order Service.
// Any class implementing this interface must provide these three methods.
using OrderService.DTOs;

namespace OrderService.Services;

public interface IOrderService
{
    Task<OrderResponseDto> PlaceOrderAsync(PlaceOrderDto dto);
    Task<OrderResponseDto?> GetOrderAsync(Guid id);
    Task<IEnumerable<OrderResponseDto>> GetOrderHistoryAsync(string customerId);
    Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync();
}
