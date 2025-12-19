using Domain.Interfaces;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Domain.Models
{
    public class Restaurant : IItemValidating
    {
        [Key]
        public string Id { get; set; } = default!;

        [Required]
        public string Name { get; set; } = "";

        [Required]
        public string OwnerEmailAddress { get; set; } = "";

        public bool Status { get; set; }

        public List<MenuItem> MenuItems { get; set; } = new();

        public List<string> GetValidators() => new() { OwnerEmailAddress };

        public string GetCardPartial() => "_RestaurantCard";
    }
}