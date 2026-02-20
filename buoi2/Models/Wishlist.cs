namespace buoi2.Models
{
    public class Wishlist
    {
        public int WishlistId { get; set; }

        // FK liên kết User
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        // FK liên kết Product
        public int ProductId { get; set; }
        public Product Product { get; set; }
    } 
}
    

