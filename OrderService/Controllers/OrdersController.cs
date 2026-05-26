using Microsoft.AspNetCore.Mvc;
using OrderService.DTOs;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // POST /api/orders — called by the customer web app when the order form is submitted
    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto)
    {
        var order = await _orderService.PlaceOrderAsync(dto);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    // GET /api/orders/{id} — customer checks their order status by order ID
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await _orderService.GetOrderAsync(id);
        return order is null ? NotFound() : Ok(order);
    }

    // GET /api/orders?customerId=xxx — returns full order history for a customer
    [HttpGet]
    public async Task<IActionResult> GetOrderHistory([FromQuery] string customerId)
    {
        var orders = await _orderService.GetOrderHistoryAsync(customerId);
        return Ok(orders);
    }
}
