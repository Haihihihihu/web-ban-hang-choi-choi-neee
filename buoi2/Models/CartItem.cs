namespace buoi2.Models
{
    public class CartItem
    {

        public int CartItemId { get; set; }  // Khóa chính
        public string UserId { get; set; }
        public int ProductId { get; set; }

        public string Name { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public int Stock { get; set; }
        public decimal SubTotal => Price * Quantity;

        public Product Product { get; set; }
        public ApplicationUser User { get; set; }
    }
}
