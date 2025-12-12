namespace AiBotOrderingSystem.Models.ViewModels;

public class CartItemDto
{
    public int MenuItemId { get; set; }
    public int Quantity { get; set; } = 1;
    public List<CartAddonDto>? Addons { get; set; }
}