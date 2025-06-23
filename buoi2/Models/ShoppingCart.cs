namespace buoi2.Models
{
    public class ShoppingCart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        public void AddItem(CartItem item)
        {
            var existingItem = Items.FirstOrDefault(i => i.ProductId == item.ProductId);
            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                Items.Add(item);
            }
        }
        public void UpdateQuantity(int productId, int quantity)
        {
            var existing = Items.FirstOrDefault(x => x.ProductId == productId);
            if (existing != null)
            {
                existing.Quantity = quantity;
            }
        }

        public decimal GetTotal() => Items.Sum(i => i.SubTotal);

        public void RemoveItem(int productId)
        {
            Items.RemoveAll(i => i.ProductId == productId);
        }
    }
}
