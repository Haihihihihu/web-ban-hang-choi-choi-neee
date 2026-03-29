using System.ComponentModel.DataAnnotations;
namespace buoi2.Models
{
    public class ProductReview
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string UserId { get; set; }     
        public string UserName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá.")]
        public string Comment { get; set; } = string.Empty;
        public int Rating { get; set; }     
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
