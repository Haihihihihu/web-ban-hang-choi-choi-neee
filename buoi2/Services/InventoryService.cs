using buoi2.Models;
using Microsoft.EntityFrameworkCore;

namespace buoi2.Services
{
    public class InventoryService
    {
        private readonly ApplicationDbContext _context;
        public InventoryService(ApplicationDbContext context) => _context = context;

        public List<Product> GetProducts() => _context.Products.ToList();

        public List<Order> GetOrders() =>
            _context.Orders.Include(o => o.OrderDetails).ToList();

        public string PlaceOrder(int productId, int quantity)
        {
            var product = _context.Products.Find(productId);
            if (product == null) return "Product not found";
            if (product.Quantity < quantity) return "Not enough stock";

            product.Quantity -= quantity;

            var order = new Order
            {
                UserId = "test-user",
                OrderDate = DateTime.Now,
                TotalPrice = product.Price * quantity,
                ShippingAddress = "Test address",
                Status = Order.OrderStatus.Pending,
                OrderDetails = new List<OrderDetail>
                {
                    new OrderDetail { ProductId = productId, Quantity = quantity, Price = product.Price }
                }
            };
            _context.Orders.Add(order);
            _context.SaveChanges();

            return "Order placed successfully!";
        }

        public string CancelOrder(int orderId)
        {
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefault(o => o.Id == orderId);

            if (order == null) return "Order not found";

            order.Status = Order.OrderStatus.Cancelled;

            foreach (var detail in order.OrderDetails)
            {
                var product = _context.Products.Find(detail.ProductId);
                if (product != null)
                {
                    product.Quantity += detail.Quantity; // trả hàng vào kho
                }
            }

            _context.SaveChanges();
            return "Order cancelled and stock restored.";
        }
    }
}
