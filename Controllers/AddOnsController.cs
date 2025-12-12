using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AiBotOrderingSystem.Data;
using AiBotOrderingSystem.Models.DbFirst;

namespace AiBotOrderingSystem.Controllers
{
    public class AddOnsController : Controller
    {
        private readonly AiBotOrderingDbContext _context;

        public AddOnsController(AiBotOrderingDbContext context)
        {
            _context = context;
        }

        // GET: AddOns
        public async Task<IActionResult> Index()
        {
            var addons = await _context.Addons.OrderBy(a => a.Name).ToListAsync();
            return View(addons);
        }

        // GET: AddOns/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: AddOns/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Addon addon)
        {
            if (!ModelState.IsValid)
                return View(addon);

            addon.CreatedAt = DateTime.Now;
            addon.UpdatedAt = DateTime.Now;
            addon.IsActive = true;

            _context.Addons.Add(addon);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: AddOns/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var addon = await _context.Addons.FindAsync(id);
            if (addon == null) return NotFound();
            return View(addon);
        }

        // POST: AddOns/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Addon addon)
        {
            var dbAddon = await _context.Addons.FindAsync(addon.Id);
            if (dbAddon == null) return NotFound();

            dbAddon.Name = addon.Name;
            dbAddon.Price = addon.Price;
            dbAddon.IsActive = addon.IsActive;
            dbAddon.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: AddOns/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var addon = await _context.Addons.FindAsync(id);
            if (addon == null) return NotFound();

            return View(addon);
        }

        // POST: AddOns/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var addon = await _context.Addons.FindAsync(id);
            if (addon == null) return NotFound();

            // Check if addon is used in MenuItemAddon
            bool isLinked = await _context.MenuItemAddons.AnyAsync(a => a.AddonId == id);

            if (isLinked)
            {
                TempData["ErrorMessage"] = "Cannot delete. This addon is mapped to menu items.";
                return RedirectToAction(nameof(Index));
            }

            _context.Addons.Remove(addon);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}