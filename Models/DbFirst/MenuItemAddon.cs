using System;
using System.Collections.Generic;

namespace AiBotOrderingSystem.Models.DbFirst;

public partial class MenuItemAddon
{
    public int Id { get; set; }

    public int? MenuItemId { get; set; }

    public int? AddonId { get; set; }

    public virtual Addon? Addon { get; set; }

    public virtual MenuItem? MenuItem { get; set; }
}
