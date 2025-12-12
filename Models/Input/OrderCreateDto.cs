using System.ComponentModel.DataAnnotations;

namespace AiBotOrderingSystem.Models.Input
{
    public class CartAddonInput
    {
        [Required]
        public int AddonId { get; set; }
        public decimal? AddonPrice { get; set; }
    }

    public class CartItemInput
    {
        [Required]
        public int MenuItemId { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; } = 1;

        public List<CartAddonInput>? Addons { get; set; }
    }

    public class OrderCreateDto
    {
        [MaxLength(200)]
        public string? CustomerName { get; set; }

        [Required]
        public int OrderType { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Cart cannot be empty")]
        public List<CartItemInput> Items { get; set; } = new();
    }
}