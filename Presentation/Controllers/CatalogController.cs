using System.Linq;
using System.Threading.Tasks;
using DataAccess.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Presentation.Controllers
{
    [AllowAnonymous]
    public class CatalogController : Controller
    {
        private readonly BulkImportDbContext _context;

        public CatalogController(BulkImportDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var restaurants = await _context.Restaurants
                .Where(r => r.Status)
                .Include(r => r.MenuItems.Where(m => m.Status))
                .ToListAsync();

            return View(restaurants);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return RedirectToAction(nameof(Index));

            var restaurant = await _context.Restaurants
                .Include(r => r.MenuItems.Where(m => m.Status))
                .FirstOrDefaultAsync(r => r.Id == id && r.Status);

            if (restaurant == null) return NotFound();
            return View(restaurant);
        }
    }
}
