using System;
using System.Collections.Generic;

namespace AiBotOrderingSystem.Models.DbFirst;

public partial class MenuItem
{
    public int Id { get; set; }

    public int? CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool? IsAvailable { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? ImageUrl { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<MenuItemAddon> MenuItemAddons { get; set; } = new List<MenuItemAddon>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
