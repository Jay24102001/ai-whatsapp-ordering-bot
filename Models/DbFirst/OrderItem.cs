using System;
using System.Collections.Generic;

namespace AiBotOrderingSystem.Models.DbFirst;

public partial class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int MenuItemId { get; set; }

    public int Quantity { get; set; }

    public decimal PriceAtOrder { get; set; }

    public decimal LineTotal { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual MenuItem MenuItem { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<OrderItemAddon> OrderItemAddons { get; set; } = new List<OrderItemAddon>();
}
