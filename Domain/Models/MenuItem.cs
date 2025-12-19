using Domain.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models
{
    public class MenuItem : IItemValidating
    {
        [Key]
        public string Id { get; set; } = default!;
        [Required]
        public string Title { get; set; } = "";
        public string? ImagePath { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Required]
        public string RestaurantId { get; set; } = default!;
        public Restaurant Restaurant { get; set; } = default!;

        public bool Status { get; set; }

        public List<string> GetValidators()
        {
            if (Restaurant == null) return new();
            return new List<string> { Restaurant.OwnerEmailAddress };
        }

        public string GetCardPartial() => "_MenuItemCard";
    }
}
