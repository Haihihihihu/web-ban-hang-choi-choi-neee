namespace buoi2.Models
{
    public class Voucher
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public bool IsForNewUser { get; set; }
        public int? CategoryId { get; set; }  // nếu muốn áp cho sản phẩm thuộc danh mục nào đó
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        public int MaxUsageCount { get; set; }
        public int UsageCount { get; set; }
    }
}