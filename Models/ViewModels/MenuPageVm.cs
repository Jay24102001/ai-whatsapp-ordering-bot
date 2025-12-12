using AiBotOrderingSystem.Models.DbFirst;

namespace AiBotOrderingSystem.Models.ViewModels
{
    public class MenuPageVm
    {
        public IEnumerable<CategoryVm> Categories { get; set; } = Enumerable.Empty<CategoryVm>();
        public IEnumerable<MenuItemVm> MenuItems { get; set; } = Enumerable.Empty<MenuItemVm>();
        public IEnumerable<AddonVm> Addons { get; set; } = Enumerable.Empty<AddonVm>(); // FIX ADDED
    }

    public class CategoryVm
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class MenuItemVm
    {
        public int Id { get; set; }
        public int? CategoryId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public List<AddonVm> Addons { get; set; } = new();
    }

    public class AddonVm
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}