using System.ComponentModel.DataAnnotations;

namespace buoi2.Models
{
    public class Category
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(50, ErrorMessage = "Tên danh mục không được quá 50 ký tự")]
        [Display(Name = "Tên danh mục")]
        public string Name { get; set; }
        
        [Display(Name = "Ảnh danh mục")]
        public string? ImageUrl { get; set; }

        public List<Product> Products { get; set; } = new List<Product>();
    }
}
