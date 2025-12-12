namespace AiBotOrderingSystem.Models.ViewModels;

public class CartAddonDto
{
    public int AddonId { get; set; }
    public decimal? AddonPrice { get; set; } // optional - server will use DB price if null
}