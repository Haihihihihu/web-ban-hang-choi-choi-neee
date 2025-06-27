using System.ComponentModel.DataAnnotations;

namespace buoi2.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required, StringLength(100)]
        public string Name { get; set; }
        [Range(0.01, 10000.00)]
        public decimal Price { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }

        public  List<ProductImage>? Images { get; set; }
        public string? ImageUrl { get; set; } // Đường dẫn đến hình ảnh đại diện

        public Category? Category { get; set; }

        public int Stock { get; set; }

<<<<<<< Updated upstream
=======
        [NotMapped]
        public double? AverageRating { get; set; }

        [NotMapped]
        public int ReviewCount { get; set; }

        public int Quantity { get; set; }
>>>>>>> Stashed changes

    }
}
