using buoi2.Extensions;
using buoi2.Models;
using buoi2.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using static buoi2.Models.Order;

namespace buoi2.Controllers
{
    [Authorize]
    public class ShoppingCartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ShoppingCartController> _logger;

        public ShoppingCartController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IProductRepository productRepository,
            ILogger<ShoppingCartController> logger)
        {
            _productRepository = productRepository;
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            ViewBag.Cart = cart; // Truyền giỏ hàng vào ViewBag
            return View(new Order());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order order)
        {
            var user = await _userManager.GetUserAsync(User);
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();

            if (!cart.Items.Any())
            {
                ModelState.AddModelError("", "Giỏ hàng trống.");
                ViewBag.Cart = cart;
                return View(order);
            }

            // Kiểm tra tồn kho
            foreach (var item in cart.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null || product.Stock < item.Quantity)
                {
                    ModelState.AddModelError("", $"Sản phẩm {item.Name} không đủ số lượng tồn kho.");
                    ViewBag.Cart = cart;
                    return View(order);
                }
            }

            order.UserId = user.Id;
            order.OrderDate = DateTime.UtcNow;
            order.TotalPrice = cart.GetTotal();
            order.Status = OrderStatus.Pending;

            order.OrderDetails = cart.Items.Select(item => new OrderDetail
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = item.Price
            }).ToList();

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // ✅ Trừ kho ngay sau khi lưu Order
            foreach (var detail in order.OrderDetails)
            {
                var product = await _productRepository.GetByIdAsync(detail.ProductId);
                if (product != null)
                {
                    product.Stock -= detail.Quantity;
                    await _productRepository.UpdateAsync(product);
                }
            }

            // Xóa giỏ hàng
            HttpContext.Session.Remove("Cart");

            return RedirectToAction("OrderCompleted", new { id = order.Id });
        }

        public async Task<IActionResult> OrderCompleted(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (order.UserId != user.Id)
            {
                return Forbid();
            }

            return View(order);
        }

        public async Task<IActionResult> Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();

            foreach (var item in cart.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    item.Stock = product.Stock;
                    if (item.Quantity > product.Stock)
                    {
                        item.Quantity = product.Stock;
                    }
                }
            }

            HttpContext.Session.SetObjectAsJson("Cart", cart);
            return View(cart);
        }
<<<<<<< Updated upstream

        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            var product = await GetProductFromDatabase(productId);
=======
        public IActionResult OrderHistory()
        {
            var userId = _userManager.GetUserId(User);

            var orders = _context.Orders
                .AsNoTracking() // để luôn lấy bản mới nhất
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();
>>>>>>> Stashed changes

            var cartItem = new CartItem
            {
                ProductId = productId,
                Name = product.Name,
                Price = product.Price,
                Quantity = quantity,
                Stock = product.Stock
            };

            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            cart.AddItem(cartItem);
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            return RedirectToAction("Index");
        }

<<<<<<< Updated upstream
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
        [HttpPost]
        [ValidateAntiForgeryToken]
=======


        [HttpPost, ValidateAntiForgeryToken]
>>>>>>> Stashed changes
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest request)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            var product = await _productRepository.GetByIdAsync(request.ProductId);
            var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

            if (product != null && cartItem != null)
            {
                cartItem.Stock = product.Stock;
                var qty = request.Quantity;

                // ✅ Validation nghiêm ngặt - không cho phép vượt quá tồn kho
                if (qty <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Số lượng phải lớn hơn 0"
                    });
                }

                if (qty > product.Stock)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Số lượng không được vượt quá tồn kho ({product.Stock})"
                    });
                }

                cart.UpdateQuantity(request.ProductId, qty);
                HttpContext.Session.SetObjectAsJson("Cart", cart);

                return Json(new
                {
                    success = true,
                    subtotal = cartItem.SubTotal,
                    total = cart.GetTotal(),
                    actualQuantity = qty
                });
            }

            return Json(new
            {
                success = false,
                message = "Sản phẩm không tồn tại"
            });
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (order.UserId != user.Id)
            {
                return Forbid();
            }

            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Preparing)
            {
                ModelState.AddModelError("", "Chỉ có thể hủy đơn hàng khi trạng thái là 'Chờ xác nhận' hoặc 'Đang chuẩn bị hàng'.");
                return RedirectToAction("OrderHistory");
            }

            // Trả lại số lượng tồn kho nếu đã trừ (trong trường hợp admin đã xác nhận và trừ kho)
            foreach (var detail in order.OrderDetails)
            {
                var product = await _productRepository.GetByIdAsync(detail.ProductId);
                if (product != null)
                {
                    product.Stock += detail.Quantity; // Trả lại kho nếu đã trừ
                    await _productRepository.UpdateAsync(product);
                }
            }

            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();

            return RedirectToAction("OrderHistory");
        }
        public async Task<IActionResult> OrderHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }
    }
}