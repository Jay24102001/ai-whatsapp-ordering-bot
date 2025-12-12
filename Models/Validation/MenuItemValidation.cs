using System.ComponentModel.DataAnnotations;
using AiBotOrderingSystem.Models.DbFirst;

namespace AiBotOrderingSystem.Models.Validation
{
    public partial class MenuItem
    {
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = null!;

        [Required]
        [Range(0.0, double.MaxValue)]
        public decimal Price { get; set; }
    }
}