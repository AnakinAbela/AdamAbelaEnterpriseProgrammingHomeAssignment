using System.Collections.Generic;

namespace Domain.Interfaces
{
    public interface IItemValidating
    {
        List<string> GetValidators();
        string GetCardPartial();
    }
}