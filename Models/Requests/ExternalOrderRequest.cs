namespace AiBotOrderingSystem.Models.Requests
{
    public class ExternalOrderRequest
    {
        public string? CustomerName { get; set; }
        public int OrderType { get; set; } // Delivery / Pickup / Table etc.
        public List<ExternalOrderItemDto> Items { get; set; } = new();
    }

    public class ExternalOrderItemDto
    {
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
        public List<int> AddonIds { get; set; } = new(); // simple for now
    }
}