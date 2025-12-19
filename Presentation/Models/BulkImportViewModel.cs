using System.Collections.Generic;
using Domain.Interfaces;

namespace Presentation.Models
{
    public class BulkImportViewModel
    {
        public List<IItemValidating> Items { get; set; } = new();
        public string? JsonInput { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
    }
}
