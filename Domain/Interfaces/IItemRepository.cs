using System.Linq;

namespace Domain.Interfaces
{
    public interface IItemsRepository
    {
        IQueryable<IItemValidating> Get();
        void Add(IItemValidating item);
    }
}