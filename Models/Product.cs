using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BulkyWeb.Utils;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BulkyWeb.Models
{
    public class Product
    {

        [Key]
        public int Id { get; set; }

        [Required]
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Size { get; set; }

        [Required]
        [Display(Name = "List Price")]
        [Range(1, 100000)]
        public double ListPrice { get; set; }

        [Required]
        [Display(Name = "Actual Price")]
        [Range(1, 100000)]
        public double Price { get; set; }

        [DisplayName("Category")]
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        [ValidateNever]
        public Category? Category { get; set; }


        [ValidateNever]
        public string? ImageUrl { get; set; }
    }
}