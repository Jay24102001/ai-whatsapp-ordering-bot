using System;
using System.Collections.Generic;
using AiBotOrderingSystem.Models.Enums;

namespace AiBotOrderingSystem.Models.DbFirst;

public partial class Order
{
    public int Id { get; set; }

    public string? CustomerName { get; set; }

    public int OrderType { get; set; }

    public OrderStatus Status { get; set; }

    public PaymentStatus PaymentStatus { get; set; }

    public PaymentMode? PaymentMode { get; set; }

    public decimal? TotalAmount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsOrderFromWhatsapp { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
