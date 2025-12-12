using Microsoft.AspNetCore.Mvc;
using AiBotOrderingSystem.Data;
using AiBotOrderingSystem.Models.Requests;
using AiBotOrderingSystem.Models.DbFirst;
using AiBotOrderingSystem.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using AiBotOrderingSystem.Hubs;

namespace AiBotOrderingSystem.Controllers;

[ApiController]
[Route("api/orders")]
public class ApiOrdersController : ControllerBase
{
    private readonly AiBotOrderingDbContext _db;
    private readonly IHubContext<OrderHub> _hub;

    public ApiOrdersController(AiBotOrderingDbContext db, IHubContext<OrderHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    // ----------------------------------------------------------
    // POST: Create External Order (Used by n8n WhatsApp Bot)
    // ----------------------------------------------------------
    [HttpPost("create-external")]
    public async Task<IActionResult> CreateExternalOrder([FromBody] ExternalOrderRequest req)
    {
        try
        {
            if (req == null || req.Items == null || req.Items.Count == 0)
                return BadRequest("Invalid request");

            using var tx = await _db.Database.BeginTransactionAsync();

            var order = new Order
            {
                CustomerName = req.CustomerName ?? "WhatsApp Customer",
                OrderType = req.OrderType,
                PaymentMode = PaymentMode.Cash,
                PaymentStatus = PaymentStatus.Pending,
                Status = OrderStatus.Received,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                TotalAmount = 0,
                IsOrderFromWhatsapp = true
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            decimal total = 0;

            foreach (var item in req.Items)
            {
                var menuItem = await _db.MenuItems.FindAsync(item.MenuItemId);
                if (menuItem == null) continue;

                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    MenuItemId = menuItem.Id,
                    Quantity = item.Quantity,
                    PriceAtOrder = menuItem.Price,
                    LineTotal = menuItem.Price * item.Quantity,
                    CreatedAt = DateTime.Now
                };

                _db.OrderItems.Add(orderItem);
                await _db.SaveChangesAsync();

                total += orderItem.LineTotal;

                if (item.AddonIds != null)
                {
                    foreach (var addonId in item.AddonIds)
                    {
                        var addon = await _db.Addons.FindAsync(addonId);
                        if (addon == null) continue;

                        var orderAddon = new OrderItemAddon
                        {
                            OrderItemId = orderItem.Id,
                            AddonId = addon.Id,
                            AddonPriceAtOrder = addon.Price,
                            CreatedAt = DateTime.Now
                        };

                        _db.OrderItemAddons.Add(orderAddon);
                        total += addon.Price;
                    }
                }
            }

            order.TotalAmount = total;
            order.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // Notify kitchen via SignalR
            await _hub.Clients.All.SendAsync("NewOrder", new { orderId = order.Id });

            return Ok(new { Success = true, OrderId = order.Id, TotalAmount = total });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = ex.Message });
        }
    }

    // ----------------------------------------------------------
    // GET: Menu List
    // ----------------------------------------------------------
    [HttpGet("menu")]
    public async Task<IActionResult> GetMenu()
    {
        var menu = await _db.MenuItems
            .Select(m => new
            {
                m.Id,
                m.Name,
                m.Price
            })
            .ToListAsync();

        return Ok(menu);
    }

    // ----------------------------------------------------------
    // GET: Order Status
    // ----------------------------------------------------------
    [HttpGet("order-status/{id:int}")]
    public async Task<IActionResult> GetOrderStatus(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound("Order not found");

        return Ok(new
        {
            order.Id,
            order.Status,
            order.TotalAmount,
            order.CreatedAt,
            order.UpdatedAt
        });
    }
}