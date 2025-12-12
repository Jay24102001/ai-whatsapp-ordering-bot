namespace AiBotOrderingSystem.Models.ViewModels;

public class CheckoutRequestDto
{
    public string? CustomerName { get; set; }
    public int OrderType { get; set; } = 0;
    public List<CartItemDto> Items { get; set; } = new();
}