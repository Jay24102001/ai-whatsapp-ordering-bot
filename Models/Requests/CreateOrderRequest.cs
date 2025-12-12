namespace AiBotOrderingSystem.Models.Requests
{
    public class CreateOrderRequest
    {
        public string? CustomerName { get; set; }
        public int OrderType { get; set; } // e.g. 1 = DineIn, 2 = Takeaway, 3 = Delivery
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; } // client-side price snapshot
        public List<OrderItemAddonDto> Addons { get; set; } = new();
    }

    public class OrderItemAddonDto
    {
        public int AddonId { get; set; }
        public int Quantity { get; set; } // quantity for this addon
        public decimal UnitPrice { get; set; } // price snapshot
    }

    public class UpdateStatusRequest
    {
        public int OrderId { get; set; }
        public int Status { get; set; }
    }
}