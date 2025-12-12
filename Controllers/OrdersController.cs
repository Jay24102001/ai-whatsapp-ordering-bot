using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using AiBotOrderingSystem.Data;
using AiBotOrderingSystem.Models.DbFirst;
using AiBotOrderingSystem.Models.Enums;
using AiBotOrderingSystem.Models.ViewModels;
using AiBotOrderingSystem.Models.Requests;
using AiBotOrderingSystem.Hubs;

namespace AiBotOrderingSystem.Controllers
{
    [Route("Orders")]
    public class OrdersController : Controller
    {
        private readonly AiBotOrderingDbContext _db;
        private readonly ILogger<OrdersController> _logger;
        private readonly IHubContext<OrderHub> _hub;

        public OrdersController(
            AiBotOrderingDbContext db,
            ILogger<OrdersController> logger,
            IHubContext<OrderHub> hub)
        {
            _db = db;
            _logger = logger;
            _hub = hub;
        }

        // =====================================================================
        // CREATE ORDER (API)
        // =====================================================================
        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] CreateOrderRequest req)
        {
            if (req == null || req.Items == null || !req.Items.Any())
                return BadRequest("Order is empty");

            var order = new Order
            {
                CustomerName = req.CustomerName,
                OrderType = req.OrderType,
                Status = OrderStatus.Received,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                TotalAmount = 0m,
                OrderItems = new List<OrderItem>()
            };

            using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                decimal total = 0m;

                foreach (var it in req.Items)
                {
                    var menuItem = await _db.MenuItems.FindAsync(it.MenuItemId);
                    if (menuItem == null)
                        return BadRequest($"Menu item {it.MenuItemId} not found.");

                    int qty = Math.Max(1, it.Quantity);
                    decimal price = menuItem.Price;
                    decimal line = price * qty;

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        MenuItemId = menuItem.Id,
                        Quantity = qty,
                        PriceAtOrder = price,
                        LineTotal = line,
                        CreatedAt = DateTime.Now,
                        OrderItemAddons = new List<OrderItemAddon>()
                    };

                    _db.OrderItems.Add(orderItem);
                    await _db.SaveChangesAsync();

                    if (it.Addons != null)
                    {
                        foreach (var ad in it.Addons)
                        {
                            var addon = await _db.Addons.FindAsync(ad.AddonId);
                            if (addon == null) continue;

                            orderItem.OrderItemAddons.Add(new OrderItemAddon
                            {
                                OrderItemId = orderItem.Id,
                                AddonId = addon.Id,
                                AddonPriceAtOrder = addon.Price,
                                CreatedAt = DateTime.Now
                            });

                            line += addon.Price;
                        }

                        orderItem.LineTotal = line;
                        _db.OrderItems.Update(orderItem);
                        await _db.SaveChangesAsync();
                    }

                    total += line;
                }

                order.TotalAmount = total;
                order.UpdatedAt = DateTime.Now;

                _db.Orders.Update(order);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                var fullOrder = await _db.Orders
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem)
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.OrderItemAddons).ThenInclude(a => a.Addon)
                    .FirstOrDefaultAsync(o => o.Id == order.Id);

                await _hub.Clients.All.SendAsync("NewOrder", new
                {
                    fullOrder.Id,
                    fullOrder.Status,
                    fullOrder.CustomerName,
                    fullOrder.TotalAmount,
                    fullOrder.CreatedAt,
                    OrderItems = fullOrder.OrderItems.Select(oi => new {
                        oi.Id,
                        oi.Quantity,
                        oi.LineTotal,
                        MenuItem = new {
                            oi.MenuItem.Id,
                            oi.MenuItem.Name,
                            oi.MenuItem.Price
                        }
                    })
                });

                return Ok(new { order.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create order");
                await tx.RollbackAsync();
                return StatusCode(500, "Failed to create order");
            }
        }

        // =====================================================================
        // MENU PAGE
        // =====================================================================
        [HttpGet("Menu")]
        public async Task<IActionResult> Menu()
        {
            var vm = new MenuPageVm();

            vm.Categories = await _db.Categories
                .Where(c => c.IsActive == true)
                .OrderBy(c => c.Name)
                .Select(c => new CategoryVm
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();

            vm.MenuItems = await _db.MenuItems
                .Where(m => m.IsAvailable == true)
                .OrderBy(m => m.Name)
                .Select(m => new MenuItemVm
                {
                    Id = m.Id,
                    CategoryId = m.CategoryId,
                    Name = m.Name,
                    Description = m.Description,
                    Price = m.Price,
                    ImageUrl = m.ImageUrl,
                    Addons = m.MenuItemAddons
                        .Where(a => a.Addon != null)
                        .Select(a => new AddonVm
                        {
                            Id = a.Addon.Id,
                            Name = a.Addon.Name,
                            Price = a.Addon.Price
                        }).ToList()
                }).ToListAsync();

            return View(vm);
        }

        // =====================================================================
        // CART PAGE
        // =====================================================================
        [HttpGet("Cart")]
        public IActionResult Cart() => View();

        // =====================================================================
        // CHECKOUT
        // =====================================================================
        [HttpPost("Checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDto req)
        {
            if (req == null || req.Items == null || !req.Items.Any())
                return BadRequest("Cart empty");

            var order = new Order
            {
                CustomerName = req.CustomerName,
                OrderType = req.OrderType,
                Status = OrderStatus.Received,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                OrderItems = new List<OrderItem>()
            };

            decimal total = 0m;

            foreach (var it in req.Items)
            {
                var item = await _db.MenuItems.FindAsync(it.MenuItemId);
                if (item == null) continue;

                int qty = Math.Max(1, it.Quantity);
                decimal line = item.Price * qty;

                var oi = new OrderItem
                {
                    MenuItemId = item.Id,
                    Quantity = qty,
                    PriceAtOrder = item.Price,
                    LineTotal = line,
                    CreatedAt = DateTime.Now,
                    OrderItemAddons = new List<OrderItemAddon>()
                };

                if (it.Addons != null)
                {
                    foreach (var a in it.Addons)
                    {
                        var addon = await _db.Addons.FindAsync(a.AddonId);
                        if (addon == null) continue;

                        oi.OrderItemAddons.Add(new OrderItemAddon
                        {
                            AddonId = addon.Id,
                            AddonPriceAtOrder = addon.Price,
                            CreatedAt = DateTime.Now
                        });

                        line += addon.Price;
                    }
                }

                oi.LineTotal = line;
                order.OrderItems.Add(oi);
                total += line;
            }

            order.TotalAmount = total;

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            var fullOrder = await _db.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.OrderItemAddons).ThenInclude(a => a.Addon)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            await _hub.Clients.All.SendAsync("NewOrder", new
            {
                fullOrder.Id,
                fullOrder.Status,
                fullOrder.CustomerName,
                fullOrder.TotalAmount,
                fullOrder.CreatedAt,
                OrderItems = fullOrder.OrderItems.Select(oi => new {
                    oi.Id,
                    oi.Quantity,
                    oi.LineTotal,
                    MenuItem = new {
                        oi.MenuItem.Id,
                        oi.MenuItem.Name,
                        oi.MenuItem.Price
                    }
                })
            });

            return Ok(new { orderId = order.Id, paymentUrl = $"/Orders/PaymentPlaceholder/{order.Id}" });
        }

        // =====================================================================
        // PAYMENT PLACEHOLDER
        // =====================================================================
        [HttpGet("PaymentPlaceholder/{id}")]
        public async Task<IActionResult> PaymentPlaceholder(int id)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();

            return View(new PaymentPlaceholderVm
            {
                OrderId = id,
                TotalAmount = order.TotalAmount ?? 0
            });
        }

        // =====================================================================
        // PAYMENT COMPLETE
        // =====================================================================
        [HttpPost("PaymentComplete")]
        public async Task<IActionResult> PaymentComplete(int orderId, string result = "success")
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            order.PaymentStatus = result == "success" ? PaymentStatus.Paid : PaymentStatus.Failed;
            if (order.PaymentStatus == PaymentStatus.Paid)
                order.Status = OrderStatus.InKitchen;

            order.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            return RedirectToAction("Details", new { id = orderId });
        }

        // =====================================================================
        // ORDER LIST
        // =====================================================================
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var list = await _db.Orders.OrderByDescending(o => o.CreatedAt).ToListAsync();
            return View(list);
        }

        // =====================================================================
        // ORDER DETAILS
        // =====================================================================
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.OrderItemAddons).ThenInclude(a => a.Addon)
                .FirstOrDefaultAsync(o => o.Id == id);

            return order == null ? NotFound() : View(order);
        }

        // =====================================================================
        // KITCHEN VIEW
        // =====================================================================
        [HttpGet("Kitchen")]
        public async Task<IActionResult> Kitchen()
        {
            var orders = await _db.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.OrderItemAddons).ThenInclude(a => a.Addon)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var vm = new
            {
                InKitchen = orders.Where(o =>
                    o.Status == OrderStatus.Received || o.Status == OrderStatus.InKitchen),
                Preparing = orders.Where(o => o.Status == OrderStatus.Preparing),
                Ready = orders.Where(o => o.Status == OrderStatus.Ready)
            };

            return View(vm);
        }

        // =====================================================================
        // UPDATE STATUS
        // =====================================================================
        [HttpPost("UpdateStatus")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest req)
        {
            if (req == null || req.OrderId <= 0)
                return BadRequest("Invalid update request");

            var order = await _db.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.OrderItemAddons).ThenInclude(a => a.Addon)
                .FirstOrDefaultAsync(o => o.Id == req.OrderId);

            if (order == null) return NotFound();

            order.Status = (OrderStatus)req.Status;
            order.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            await _hub.Clients.All.SendAsync("OrderUpdated", new
            {
                order.Id,
                order.Status,
                order.CustomerName,
                order.TotalAmount,
                order.CreatedAt,
                OrderItems = order.OrderItems.Select(oi => new
                {
                    oi.Id,
                    oi.Quantity,
                    oi.LineTotal,
                    MenuItem = new
                    {
                        oi.MenuItem.Id,
                        oi.MenuItem.Name,
                        oi.MenuItem.Price
                    }
                })
            });

            return Ok();
        }

        // =====================================================================
        // PUNCH PAGE
        // =====================================================================
        [HttpGet("Punch")]
        public async Task<IActionResult> Punch()
        {
            var vm = new MenuPageVm();

            vm.Categories = await _db.Categories
                .Where(c => c.IsActive == true)
                .OrderBy(c => c.Name)
                .Select(c => new CategoryVm
                {
                    Id = c.Id,
                    Name = c.Name
                }).ToListAsync();

            vm.MenuItems = await _db.MenuItems
                .Where(m => m.IsAvailable == true)
                .OrderBy(m => m.Name)
                .Select(m => new MenuItemVm
                {
                    Id = m.Id,
                    CategoryId = m.CategoryId,
                    Name = m.Name,
                    Description = m.Description,
                    Price = m.Price,
                    ImageUrl = m.ImageUrl,
                    Addons = m.MenuItemAddons
                        .Where(a => a.Addon != null)
                        .Select(a => new AddonVm
                        {
                            Id = a.Addon.Id,
                            Name = a.Addon.Name,
                            Price = a.Addon.Price
                        })
                        .ToList()
                }).ToListAsync();

            return View(vm);
        }
    }
}