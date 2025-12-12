using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AiBotOrderingSystem.Models.DbFirst;
using AiBotOrderingSystem.Data;

namespace AiBotOrderingSystem.Controllers
{
    public class MenuItemsController : Controller
    {
        private readonly AiBotOrderingDbContext _context;
        private readonly IWebHostEnvironment _env;

        public MenuItemsController(AiBotOrderingDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: MenuItems
        public async Task<IActionResult> Index()
        {
            var items = await _context.MenuItems
                .Include(m => m.Category)
                .Include(m => m.MenuItemAddons)
                    .ThenInclude(ma => ma.Addon)
                .OrderBy(m => m.Name)
                .ToListAsync();

            return View(items);
        }

        // GET: MenuItems/Create
        public IActionResult Create()
        {
            ViewBag.Categories = _context.Categories.OrderBy(c => c.Name).ToList();
            ViewBag.AddOns = _context.Addons.Where(a => a.IsActive == true).OrderBy(a => a.Name).ToList();
            return View();
        }

        // POST: MenuItems/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuItem menuItem, IFormFile? ImageFile, int[] SelectedAddOns)
        {
            // IMAGE UPLOAD
            if (ImageFile != null)
            {
                string folder = Path.Combine(_env.WebRootPath, "images/menu");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                string filePath = Path.Combine(folder, fileName);

                using var fs = new FileStream(filePath, FileMode.Create);
                await ImageFile.CopyToAsync(fs);

                menuItem.ImageUrl = "/images/menu/" + fileName;
            }

            // SAVE MENU ITEM
            _context.MenuItems.Add(menuItem);
            await _context.SaveChangesAsync(); // ID generated

            // SAVE ADDONS
            if (SelectedAddOns != null && SelectedAddOns.Any())
            {
                foreach (var addOnId in SelectedAddOns)
                {
                    _context.MenuItemAddons.Add(new MenuItemAddon
                    {
                        MenuItemId = menuItem.Id,
                        AddonId = addOnId
                    });
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: MenuItems/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.MenuItems
                .Include(m => m.MenuItemAddons)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (item == null) return NotFound();

            ViewBag.Categories = _context.Categories.OrderBy(c => c.Name).ToList();
            ViewBag.AddOns = _context.Addons.Where(a => a.IsActive == true).OrderBy(a => a.Name).ToList();
            ViewBag.SelectedAddOns = item.MenuItemAddons.Select(a => a.AddonId ?? 0).ToList();

            return View(item);
        }

        // POST: MenuItems/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MenuItem menuItem, IFormFile? ImageFile, int[] SelectedAddOns)
        {
            var dbItem = await _context.MenuItems
                .Include(m => m.MenuItemAddons)
                .FirstOrDefaultAsync(m => m.Id == menuItem.Id);

            if (dbItem == null) return NotFound();

            // Update fields
            dbItem.Name = menuItem.Name;
            dbItem.Description = menuItem.Description;
            dbItem.CategoryId = menuItem.CategoryId;
            dbItem.Price = menuItem.Price;

            // Update image
            if (ImageFile != null)
            {
                string folder = Path.Combine(_env.WebRootPath, "images/menu");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                string path = Path.Combine(folder, fileName);

                using var fs = new FileStream(path, FileMode.Create);
                await ImageFile.CopyToAsync(fs);

                dbItem.ImageUrl = "/images/menu/" + fileName;
            }

            // Remove old add-ons
            _context.MenuItemAddons.RemoveRange(dbItem.MenuItemAddons);

            // Add new add-ons
            if (SelectedAddOns != null && SelectedAddOns.Any())
            {
                foreach (var addOnId in SelectedAddOns)
                {
                    _context.MenuItemAddons.Add(new MenuItemAddon
                    {
                        MenuItemId = dbItem.Id,
                        AddonId = addOnId
                    });
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: MenuItems/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var item = await _context.MenuItems
                .Include(m => m.Category)
                .Include(m => m.MenuItemAddons)
                    .ThenInclude(ma => ma.Addon)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (item == null) return NotFound();

            return View(item);
        }

        // GET: MenuItems/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (item == null) return NotFound();

            return View(item);
        }

        // POST: MenuItems/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return NotFound();

            // Optional: delete image
            if (!string.IsNullOrEmpty(item.ImageUrl))
            {
                try
                {
                    var filePath = Path.Combine(
                        _env.WebRootPath,
                        item.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
                    );

                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
                catch { }
            }

            _context.MenuItems.Remove(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // AJAX: Fetch addons for a menu item
        [HttpGet]
        public async Task<IActionResult> GetAddonsForItem(int id)
        {
            var addonIds = await _context.MenuItemAddons
                .Where(ma => ma.MenuItemId == id)
                .Select(ma => ma.AddonId)
                .ToListAsync();

            var addons = await _context.Addons
                .Where(a => addonIds.Contains(a.Id) && (a.IsActive))
                .Select(a => new
                {
                    id = a.Id,
                    name = a.Name,
                    price = a.Price
                })
                .ToListAsync();

            return Json(addons);
        }
    }
}