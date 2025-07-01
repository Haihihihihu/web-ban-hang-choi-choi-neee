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

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            // Lấy cart từ database cho user đăng nhập
            var cartItems = await _context.CartItems
                .Where(ci => ci.UserId == user.Id)
                .Include(ci => ci.Product)
                .ToListAsync();
            var cart = new ShoppingCart { Items = cartItems };

            bool isNewUser = !_context.Orders.Any(o => o.UserId == user.Id);
            decimal cartTotal = cart.GetTotal();

            var allVouchers = await _context.Vouchers
                .Where(v => v.IsActive && v.ExpiryDate >= DateTime.Now && v.UsageCount < v.MaxUsageCount)
                .OrderByDescending(v => v.DiscountAmount ?? 0)
                .ThenByDescending(v => v.DiscountPercent ?? 0)
                .ToListAsync();

            var applicableVouchers = new List<dynamic>();
            var notApplicableVouchers = new List<dynamic>();
            foreach (var voucher in allVouchers)
            {
                bool canUse = true;
                string reason = "";
                if (voucher.MinOrderAmount.HasValue && cartTotal < voucher.MinOrderAmount.Value)
                {
                    canUse = false;
                    reason = $"Cần đặt tối thiểu {voucher.MinOrderAmount.Value:N0} ₫";
                }
                if (voucher.IsForNewUser && !isNewUser)
                {
                    canUse = false;
                    reason = "Chỉ dành cho khách hàng mới";
                }
                var voucherInfo = new
                {
                    voucher.Id,
                    voucher.Code,
                    voucher.DiscountAmount,
                    voucher.DiscountPercent,
                    voucher.MinOrderAmount,
                    voucher.IsForNewUser,
                    voucher.ExpiryDate,
                    CanUse = canUse,
                    Reason = reason,
                    UsageRemaining = voucher.MaxUsageCount - voucher.UsageCount
                };
                if (canUse)
                    applicableVouchers.Add(voucherInfo);
                else
                    notApplicableVouchers.Add(voucherInfo);
            }
            ViewBag.ApplicableVouchers = applicableVouchers;
            ViewBag.NotApplicableVouchers = notApplicableVouchers;
            ViewBag.Cart = cart;
            ViewBag.IsNewUser = isNewUser;
            return View(new Order());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order order, string VoucherCode)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");
            var cartItems = await _context.CartItems
                .Where(ci => ci.UserId == user.Id)
                .Include(ci => ci.Product)
                .ToListAsync();
            var cart = new ShoppingCart { Items = cartItems };
            if (!cart.Items.Any())
            {
                ModelState.AddModelError("", "Giỏ hàng trống.");
                ViewBag.Cart = cart;
                // Load lại voucher cho view
                bool isNewUser = !_context.Orders.Any(o => o.UserId == user.Id);
                decimal cartTotal = cart.GetTotal();
                var allVouchers = await _context.Vouchers
                    .Where(v => v.IsActive && v.ExpiryDate >= DateTime.Now && v.UsageCount < v.MaxUsageCount)
                    .OrderByDescending(v => v.DiscountAmount ?? 0)
                    .ThenByDescending(v => v.DiscountPercent ?? 0)
                    .ToListAsync();
                var applicableVouchers = new List<dynamic>();
                var notApplicableVouchers = new List<dynamic>();
                foreach (var voucher in allVouchers)
                {
                    bool canUse = true;
                    string reason = "";
                    if (voucher.MinOrderAmount.HasValue && cartTotal < voucher.MinOrderAmount.Value)
                    {
                        canUse = false;
                        reason = $"Cần đặt tối thiểu {voucher.MinOrderAmount.Value:N0} ₫";
                    }
                    if (voucher.IsForNewUser && !isNewUser)
                    {
                        canUse = false;
                        reason = "Chỉ dành cho khách hàng mới";
                    }
                    var voucherInfo = new
                    {
                        voucher.Id,
                        voucher.Code,
                        voucher.DiscountAmount,
                        voucher.DiscountPercent,
                        voucher.MinOrderAmount,
                        voucher.IsForNewUser,
                        voucher.ExpiryDate,
                        CanUse = canUse,
                        Reason = reason,
                        UsageRemaining = voucher.MaxUsageCount - voucher.UsageCount
                    };
                    if (canUse)
                        applicableVouchers.Add(voucherInfo);
                    else
                        notApplicableVouchers.Add(voucherInfo);
                }
                ViewBag.ApplicableVouchers = applicableVouchers;
                ViewBag.NotApplicableVouchers = notApplicableVouchers;
                ViewBag.IsNewUser = isNewUser;
                return View(order);
            }
            decimal totalPrice = cart.GetTotal();
            if (!string.IsNullOrEmpty(VoucherCode))
            {
                var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == VoucherCode && v.IsActive);
                if (voucher != null)
                {
                    bool canUseVoucher = true;
                    if (voucher.ExpiryDate < DateTime.Now)
                        canUseVoucher = false;
                    if (voucher.UsageCount >= voucher.MaxUsageCount)
                        canUseVoucher = false;
                    if (voucher.MinOrderAmount.HasValue && totalPrice < voucher.MinOrderAmount.Value)
                        canUseVoucher = false;
                    if (voucher.IsForNewUser)
                    {
                        bool isNewUser = !_context.Orders.Any(o => o.UserId == user.Id);
                        if (!isNewUser)
                            canUseVoucher = false;
                    }
                    if (canUseVoucher)
                    {
                        if (voucher.DiscountAmount.HasValue && voucher.DiscountAmount.Value > 0)
                        {
                            totalPrice -= voucher.DiscountAmount.Value;
                        }
                        else if (voucher.DiscountPercent.HasValue && voucher.DiscountPercent.Value > 0)
                        {
                            totalPrice *= 1 - (voucher.DiscountPercent.Value / 100m);
                        }
                        voucher.UsageCount++;
                        _context.Vouchers.Update(voucher);
                    }
                    else
                    {
                        ModelState.AddModelError("", "Voucher không hợp lệ hoặc không đủ điều kiện sử dụng.");
                        // Load lại voucher cho view
                        bool isNewUser = !_context.Orders.Any(o => o.UserId == user.Id);
                        decimal cartTotal = cart.GetTotal();
                        var allVouchers = await _context.Vouchers
                            .Where(v => v.IsActive && v.ExpiryDate >= DateTime.Now && v.UsageCount < v.MaxUsageCount)
                            .OrderByDescending(v => v.DiscountAmount ?? 0)
                            .ThenByDescending(v => v.DiscountPercent ?? 0)
                            .ToListAsync();
                        var applicableVouchers = new List<dynamic>();
                        var notApplicableVouchers = new List<dynamic>();
                        foreach (var v in allVouchers)
                        {
                            bool canUse = true;
                            string reason = "";
                            if (v.MinOrderAmount.HasValue && cartTotal < v.MinOrderAmount.Value)
                            {
                                canUse = false;
                                reason = $"Cần đặt tối thiểu {v.MinOrderAmount.Value:N0} ₫";
                            }
                            if (v.IsForNewUser && !isNewUser)
                            {
                                canUse = false;
                                reason = "Chỉ dành cho khách hàng mới";
                            }
                            var voucherInfo = new
                            {
                                v.Id,
                                v.Code,
                                v.DiscountAmount,
                                v.DiscountPercent,
                                v.MinOrderAmount,
                                v.IsForNewUser,
                                v.ExpiryDate,
                                CanUse = canUse,
                                Reason = reason,
                                UsageRemaining = v.MaxUsageCount - v.UsageCount
                            };
                            if (canUse)
                                applicableVouchers.Add(voucherInfo);
                            else
                                notApplicableVouchers.Add(voucherInfo);
                        }
                        ViewBag.ApplicableVouchers = applicableVouchers;
                        ViewBag.NotApplicableVouchers = notApplicableVouchers;
                        ViewBag.Cart = cart;
                        ViewBag.IsNewUser = isNewUser;
                        return View(order);
                    }
                }
            }
            totalPrice = Math.Max(0, totalPrice);
            order.UserId = user.Id;
            order.OrderDate = DateTime.UtcNow;
            order.TotalPrice = totalPrice;
            order.VoucherCode = VoucherCode;
            order.Status = Order.OrderStatus.Pending;
            order.OrderDetails = cart.Items.Select(item => new OrderDetail
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = item.Price
            }).ToList();
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
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
            if (user == null || order.UserId != user.Id)
            {
                return Forbid();
            }

            return View(order);
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                // Lấy cart từ database
                var cartItems = await _context.CartItems
                    .Where(ci => ci.UserId == user.Id)
                    .Include(ci => ci.Product)
                    .ToListAsync();
                var cart = new ShoppingCart { Items = cartItems };
                return View(cart);
            }
            else
            {
                // Guest: lấy cart từ session
                var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
                return View(cart);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            var user = await _userManager.GetUserAsync(User);
            var product = await GetProductFromDatabase(productId);
            if (product == null || product.Stock <= 0)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại hoặc đã hết hàng!" });
            }
            if (user != null)
            {
                // Cart in database
                var existingItem = await _context.CartItems.FirstOrDefaultAsync(i => i.UserId == user.Id && i.ProductId == productId);
                if (existingItem != null)
                {
                    if (existingItem.Quantity + quantity > product.Stock)
                    {
                        return Json(new { success = false, message = $"Chỉ còn {product.Stock} sản phẩm trong kho." });
                    }
                    existingItem.Quantity += quantity;
                }
                else
                {
                    _context.CartItems.Add(new CartItem
                    {
                        UserId = user.Id,
                        ProductId = productId,
                        Name = product.Name,
                        Price = product.Price,
                        Quantity = quantity,
                        Stock = product.Stock
                    });
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            else
            {
                // Guest: cart in session
                var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
                var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (existingItem != null)
                {
                    if (existingItem.Quantity + quantity > product.Stock)
                    {
                        return Json(new { success = false, message = $"Chỉ còn {product.Stock} sản phẩm trong kho." });
                    }
                    existingItem.Quantity += quantity;
                }
                else
                {
                    cart.AddItem(new CartItem
                    {
                        ProductId = productId,
                        Name = product.Name,
                        Price = product.Price,
                        Quantity = quantity,
                        Stock = product.Stock
                    });
                }
                HttpContext.Session.SetObjectAsJson("Cart", cart);
                return Json(new { success = true });
            }
        }

        public IActionResult OrderHistory()
        {
            var userId = _userManager.GetUserId(User);

            var orders = _context.Orders
                .AsNoTracking()
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var item = await _context.CartItems.FirstOrDefaultAsync(i => i.UserId == user.Id && i.ProductId == productId);
                if (item != null)
                {
                    _context.CartItems.Remove(item);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("Index");
            }
            else
            {
                var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
                if (cart is not null)
                {
                    cart.RemoveItem(productId);
                    HttpContext.Session.SetObjectAsJson("Cart", cart);
                }
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            var product = await _productRepository.GetByIdAsync(request.ProductId);
            if (user != null)
            {
                var cartItem = await _context.CartItems.FirstOrDefaultAsync(i => i.UserId == user.Id && i.ProductId == request.ProductId);
                if (product != null && cartItem != null)
                {
                    cartItem.Stock = product.Stock;
                    var qty = request.Quantity;
                    if (qty <= 0)
                    {
                        return Json(new { success = false, message = "Số lượng phải lớn hơn 0" });
                    }
                    if (qty > product.Stock)
                    {
                        return Json(new { success = false, message = $"Số lượng không được vượt quá tồn kho ({product.Stock})" });
                    }
                    cartItem.Quantity = qty;
                    await _context.SaveChangesAsync();
                    var subtotal = cartItem.SubTotal;
                    var total = await _context.CartItems.Where(i => i.UserId == user.Id).SumAsync(i => i.Quantity * i.Price);
                    return Json(new { success = true, subtotal, total, actualQuantity = qty });
                }
                return Json(new { success = false, message = "Sản phẩm không tồn tại" });
            }
            else
            {
                var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
                var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
                if (product != null && cartItem != null)
                {
                    cartItem.Stock = product.Stock;
                    var qty = request.Quantity;
                    if (qty <= 0)
                    {
                        return Json(new { success = false, message = "Số lượng phải lớn hơn 0" });
                    }
                    if (qty > product.Stock)
                    {
                        return Json(new { success = false, message = $"Số lượng không được vượt quá tồn kho ({product.Stock})" });
                    }
                    cart.UpdateQuantity(request.ProductId, qty);
                    HttpContext.Session.SetObjectAsJson("Cart", cart);
                    return Json(new { success = true, subtotal = cartItem.SubTotal, total = cart.GetTotal(), actualQuantity = qty });
                }
                return Json(new { success = false, message = "Sản phẩm không tồn tại" });
            }
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
            if (user == null || order.UserId != user.Id)
            {
                return Forbid();
            }

            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Preparing)
            {
                ModelState.AddModelError("", "Chỉ có thể hủy đơn hàng khi trạng thái là 'Chờ xác nhận' hoặc 'Đang chuẩn bị hàng'.");
                return RedirectToAction("OrderHistory");
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

            return RedirectToAction("OrderHistory");
        }
    }
}