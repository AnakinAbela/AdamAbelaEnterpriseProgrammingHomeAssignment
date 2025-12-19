using System;
using System.Linq;
using System.Threading.Tasks;
using DataAccess.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Presentation.Controllers
{
    [Authorize]
    public class ApprovalsController : Controller
    {
        private readonly BulkImportDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ApprovalsController(BulkImportDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Restaurants()
        {
            var pending = await _context.Restaurants
                .Where(r => !r.Status)
                .ToListAsync();
            return View(pending);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRestaurant(string id)
        {
            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant == null) return NotFound();

            restaurant.Status = true;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Restaurants));
        }

        public async Task<IActionResult> MenuItems()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var normalizedEmail = user.Email?.ToLower() ?? string.Empty;

            var pending = await _context.MenuItems
                .Include(m => m.Restaurant)
                .Where(m => !m.Status &&
                            m.Restaurant != null &&
                            m.Restaurant.OwnerEmailAddress != null &&
                            m.Restaurant.OwnerEmailAddress.ToLower() == normalizedEmail)
                .ToListAsync();

            return View(pending);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveMenuItem(string id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var menuItem = await _context.MenuItems
                .Include(m => m.Restaurant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menuItem == null) return NotFound();

            if (menuItem.Restaurant?.OwnerEmailAddress == null ||
                !menuItem.Restaurant.OwnerEmailAddress.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            menuItem.Status = true;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MenuItems));
        }
    }
}
