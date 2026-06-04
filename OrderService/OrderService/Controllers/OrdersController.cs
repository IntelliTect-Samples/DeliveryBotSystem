// Handles incoming HTTP requests for the Order Service.
// Routes: POST /api/orders, GET /api/orders/{id}, GET /api/orders?customerId=
// No business logic here — just receives requests and delegates to OrderService.
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

    // GET /api/orders — returns all orders (admin view, issue #53)
    // GET /api/orders?customerId=xxx — returns full order history for one customer
    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] string? customerId)
    {
        var orders = string.IsNullOrWhiteSpace(customerId)
            ? await _orderService.GetAllOrdersAsync()
            : await _orderService.GetOrderHistoryAsync(customerId);
        return Ok(orders);
    }
}
