using Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Linq;

namespace DataAccess.Repositories
{
    public class ItemsInMemoryRepository : IItemsRepository
    {
        private readonly IMemoryCache _cache;
        private const string CACHE_KEY = "IMPORTED_ITEMS";

        public ItemsInMemoryRepository(IMemoryCache cache) 
            => _cache = cache;

        public IQueryable<IItemValidating> Get()
        {
            var list = _cache.GetOrCreate(CACHE_KEY, entry => new List<IItemValidating>())!;
            return list.AsQueryable();
        }

        public void Add(IItemValidating item)
        {
            var list = _cache.GetOrCreate(CACHE_KEY, entry => new List<IItemValidating>())!;
            list.Add(item);
            _cache.Set(CACHE_KEY, list);
        }
    }
}
