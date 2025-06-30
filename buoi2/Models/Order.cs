using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
namespace buoi2.Models
{
    public class Order
    {
        public enum OrderStatus
        {
            Pending = 0,      // Chờ xác nhận
            Preparing = 1,    // Đang chuẩn bị hàng
            Shipping = 2,     // Đang giao hàng
            Delivered = 3,    // Đã giao hàng
            Cancelled = 4
        }
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
        public string UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string ShippingAddress { get; set; } 
        public OrderStatus Status { get; set; }
        public string Notes { get; set; }

        public string? VoucherCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalTotal => TotalPrice - DiscountAmount;

        [ForeignKey("UserId")]
        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; }
        public List<OrderDetail> OrderDetails { get; set; }
        [AllowNull]
        public string? PaymentMethod { get; set; }
    }
}
