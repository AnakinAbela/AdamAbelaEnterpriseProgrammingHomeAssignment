using Domain.Interfaces;
using Domain.Models;
using DataAccess.Context;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories
{
    public class ItemsDbRepository : IItemsRepository
    {
        private readonly BulkImportDbContext _context;

        public ItemsDbRepository(BulkImportDbContext context) 
            => _context = context;

        public IQueryable<IItemValidating> Get()
        {
            var restaurants = _context.Restaurants.Cast<IItemValidating>();
            var menuItems = _context.MenuItems.Include(m => m.Restaurant).Cast<IItemValidating>();
            return restaurants.Concat(menuItems).AsQueryable();
        }

        public void Add(IItemValidating item)
        {
            if (item is Restaurant restaurant)
            {
                if (_context.Restaurants.Any(r => r.Id == restaurant.Id)) return;
                _context.Restaurants.Add(restaurant);
            }
            else if (item is MenuItem menuItem)
            {
                if (_context.MenuItems.Any(m => m.Id == menuItem.Id)) return;
                menuItem.Restaurant = null!; // detach navigation, rely on RestaurantId
                _context.MenuItems.Add(menuItem);
            }

            _context.SaveChanges();
        }
    }
}
