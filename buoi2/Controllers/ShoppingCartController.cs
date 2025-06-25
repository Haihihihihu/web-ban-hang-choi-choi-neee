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

        public ShoppingCartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IProductRepository productRepository)
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IProductRepository _productRepository;

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

        public IActionResult Checkout()
        public async Task<IActionResult> RemoveFromCart(int productId)

        {
            var user = await _userManager.GetUserAsync(User);
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.UserId == user.Id && ci.ProductId == productId);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var user = await _userManager.GetUserAsync(User);

            var cartItems = await _context.CartItems
                .Where(ci => ci.UserId == user.Id)
                .Include(ci => ci.Product)
                .ToListAsync();

            // Nếu có sản phẩm nào số lượng <= 0 thì không cho vào view thanh toán
            var invalidItems = cartItems.Where(ci => ci.Quantity <= 0).ToList();
            if (invalidItems.Any())
            {
                TempData["Error"] = "Có sản phẩm trong giỏ hàng có số lượng không hợp lệ . Vui lòng xóa hoặc chỉnh lại.";
                return RedirectToAction("Index"); // Về lại trang giỏ hàng
            }

            var shoppingCart = new ShoppingCart { Items = cartItems };
            var order = new Order();

            ViewBag.Cart = shoppingCart;
            return View(order);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order order)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");

            if (cart == null || !cart.Items.Any())
            var user = await _userManager.GetUserAsync(User);

            var cartItems = await _context.CartItems
                .Where(ci => ci.UserId == user.Id)
                .ToListAsync();

            // Loại bỏ sản phẩm số lượng = 0
            var invalidItems = cartItems.Where(ci => ci.Quantity <= 0).ToList();

            if (!invalidItems.Any())
            {
                ModelState.AddModelError("", "Không có sản phẩm nào hợp lệ để đặt hàng (số lượng phải > 0).");
                ViewBag.Cart = new ShoppingCart { Items = invalidItems };
                return View(order);
            }

            var user = await _userManager.GetUserAsync(User);
            order.UserId = user.Id;
            order.OrderDate = DateTime.UtcNow;
            order.TotalPrice = cart.Items.Sum(i => i.Price * i.Quantity);
            order.OrderDetails = cart.Items.Select(i => new OrderDetail
            // Kiểm tra tồn kho
            foreach (var item in invalidItems)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null || product.Stock < item.Quantity)
                {
                    ModelState.AddModelError("", $"Sản phẩm {item.Name} không đủ tồn kho.");
                    ViewBag.Cart = new ShoppingCart { Items = invalidItems };
                    return View(order);
                }
            }

            // Tiến hành tạo Order
            order.UserId = user.Id;
            order.OrderDate = DateTime.UtcNow;
            order.TotalPrice = invalidItems.Sum(i => i.Quantity * i.Price);
            order.Status = OrderStatus.Pending;

            order.OrderDetails = invalidItems.Select(ci => new OrderDetail
            {
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                Price = ci.Price
            }).ToList();

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            HttpContext.Session.Remove("Cart");
            return View("OrderCompleted", order.Id);
        }



        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            // Giả sử bạn có phương thức lấy thông tin sản phẩm từ productId
            var product = await GetProductFromDatabase(productId);

            var cartItem = new CartItem
            {
                ProductId = productId,
                Name = product.Name,
                Price = product.Price,
                Quantity = quantity
            };

            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ??
                       new ShoppingCart();
            cart.AddItem(cartItem);
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            return RedirectToAction("Index");
        }

        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ??
                       new ShoppingCart();
            return View(cart);
        }

        // Các actions khác...
        private async Task<Product> GetProductFromDatabase(int productId)

            foreach (var detail in order.OrderDetails)
            {
                var product = await _productRepository.GetByIdAsync(detail.ProductId);
                if (product != null)
                {
                    product.Stock -= detail.Quantity;
                    await _productRepository.UpdateAsync(product);
                }
            }

            _context.CartItems.RemoveRange(cartItems); // Xóa hết giỏ cũ
            await _context.SaveChangesAsync();

            return RedirectToAction("OrderCompleted", new { id = order.Id });
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

        public IActionResult RemoveFromCart(int productId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");

            if (cart is not null)
            {
                cart.RemoveItem(productId);

                // Lưu lại giỏ hàng vào Session sau khi đã xóa mục
                HttpContext.Session.SetObjectAsJson("Cart", cart);
            }

            return RedirectToAction("Index");
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

        private async Task<Product> GetProductFromDatabase(int productId)
        {
            return await _productRepository.GetByIdAsync(productId);
        }

    }
}
