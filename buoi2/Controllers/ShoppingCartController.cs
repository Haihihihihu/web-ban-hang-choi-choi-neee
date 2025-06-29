using buoi2.Extensions;
using buoi2.Models;
using buoi2.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static buoi2.Models.Order;

namespace buoi2.Controllers
{
    [Authorize]
    public class ShoppingCartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShoppingCartController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IProductRepository productRepository)
        {
            _context = context;
            _userManager = userManager;
            _productRepository = productRepository;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var cartItems = await _context.CartItems
                .Where(ci => ci.UserId == user.Id)
                .ToListAsync();

            var shoppingCart = new ShoppingCart { Items = cartItems };
            return View(shoppingCart);
        }

        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            var user = await _userManager.GetUserAsync(User);
            var product = await _productRepository.GetByIdAsync(productId);

            if (product == null || product.Stock <= 0)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại hoặc đã hết hàng!" });
            }

            var existingCart = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.UserId == user.Id && ci.ProductId == productId);

            if (existingCart != null)
            {
                existingCart.Quantity = Math.Min(existingCart.Quantity + quantity, product.Stock);
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    UserId = user.Id,
                    ProductId = productId,
                    Quantity = quantity,
                    Name = product.Name,
                    Price = product.Price,
                    Stock = product.Stock
                });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        public IActionResult RemoveFromCart(int productId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart is not null)
            {
                cart.RemoveItem(productId);
                HttpContext.Session.SetObjectAsJson("Cart", cart);
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var user = await _userManager.GetUserAsync(User);
            var cartItems = await _context.CartItems
                .Where(ci => ci.UserId == user.Id)
                .Include(ci => ci.Product)
                .ToListAsync();

            var invalidItems = cartItems.Where(ci => ci.Quantity <= 0).ToList();
            if (invalidItems.Any())
            {
                TempData["Error"] = "Có sản phẩm trong giỏ hàng có số lượng không hợp lệ. Vui lòng xóa hoặc chỉnh lại.";
                return RedirectToAction("Index");
            }

            ViewBag.Cart = new ShoppingCart { Items = cartItems };
            return View(new Order());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order order)
        {
            var user = await _userManager.GetUserAsync(User);
            var cartItems = await _context.CartItems
                .Where(ci => ci.UserId == user.Id)
                .ToListAsync();

            var validItems = cartItems.Where(ci => ci.Quantity > 0).ToList();

            foreach (var item in validItems)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null || product.Stock < item.Quantity)
                {
                    ModelState.AddModelError("", $"Sản phẩm {item.Name} không đủ tồn kho.");
                    ViewBag.Cart = new ShoppingCart { Items = validItems };
                    return View(order);
                }
            }

            order.UserId = user.Id;
            order.OrderDate = DateTime.UtcNow;
            order.TotalPrice = validItems.Sum(i => i.Quantity * i.Price);
            order.Status = OrderStatus.Pending;
            order.OrderDetails = validItems.Select(ci => new OrderDetail
            {
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                Price = ci.Price
            }).ToList();

            _context.Orders.Add(order);

            foreach (var detail in order.OrderDetails)
            {
                var product = await _productRepository.GetByIdAsync(detail.ProductId);
                if (product != null)
                {
                    product.Stock -= detail.Quantity;
                    await _productRepository.UpdateAsync(product);
                }
            }

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return View("OrderCompleted", order);
        }

        public async Task<IActionResult> OrderCompleted(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            var user = await _userManager.GetUserAsync(User);
            if (order == null || order.UserId != user.Id) return Forbid();

            return View(order);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);

            var user = await _userManager.GetUserAsync(User);
            if (order == null || order.UserId != user.Id) return NotFound();

            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Preparing)
            {
                TempData["Error"] = "Chỉ hủy được đơn hàng chờ xác nhận hoặc đang chuẩn bị.";
                return RedirectToAction(nameof(OrderHistory));
            }

            foreach (var detail in order.OrderDetails)
            {
                var product = await _productRepository.GetByIdAsync(detail.ProductId);
                if (product != null)
                {
                    product.Stock += detail.Quantity;
                    await _productRepository.UpdateAsync(product);
                }
            }

            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(OrderHistory));
        }

        public async Task<IActionResult> OrderHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            var product = await _productRepository.GetByIdAsync(request.ProductId);
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.ProductId == request.ProductId && ci.UserId == user.Id);

            if (product == null || cartItem == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại" });
            }

            var qty = request.Quantity;
            if (qty <= 0)
            {
                return Json(new { success = false, message = "Số lượng phải lớn hơn 0" });
            }

            if (qty > product.Stock)
            {
                qty = product.Stock;
                return Json(new { success = false, message = $"Chỉ còn {product.Stock} sản phẩm {product.Name}.", actualQuantity = qty });
            }

            cartItem.Quantity = qty;
            cartItem.Stock = product.Stock;
            await _context.SaveChangesAsync();

            var subtotal = cartItem.SubTotal;
            var total = await _context.CartItems
                .Where(ci => ci.UserId == user.Id)
                .SumAsync(ci => ci.Quantity * ci.Price);

            return Json(new { success = true, subtotal, total, actualQuantity = qty });
        }

        public class UpdateQuantityRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }
    }
}
