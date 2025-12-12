using System;
using System.Collections.Generic;

namespace AiBotOrderingSystem.Models.DbFirst;

public partial class OrderItemAddon
{
    public int Id { get; set; }

    public int OrderItemId { get; set; }

    public int AddonId { get; set; }

    public decimal AddonPriceAtOrder { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Addon Addon { get; set; } = null!;

    public virtual OrderItem OrderItem { get; set; } = null!;
}
