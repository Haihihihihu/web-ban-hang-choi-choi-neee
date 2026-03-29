using buoi2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace buoi2.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WishlistController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var wishlists = await _context.Wishlists
                .Include(w => w.Product)
                .Where(w => w.UserId == user.Id)
                .ToListAsync();

            return View(wishlists);
        }

        // Thêm sản phẩm vào wishlist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId)
        {
            var user = await _userManager.GetUserAsync(User);

            var exists = _context.Wishlists.Any(w => w.ProductId == productId && w.UserId == user.Id);
            if (!exists)
            {
                _context.Wishlists.Add(new Wishlist { UserId = user.Id, ProductId = productId });
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        // Xóa sản phẩm khỏi wishlist
        public async Task<IActionResult> Remove(int id) // id này là WishlistId
        {
            var wishlist = await _context.Wishlists.FindAsync(id);
            if (wishlist != null)
            {
                _context.Wishlists.Remove(wishlist);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> MoveToCart(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var wishlist = await _context.Wishlists
                .Include(w => w.Product)
                .FirstOrDefaultAsync(w => w.WishlistId == id && w.UserId == user.Id);

            if (wishlist != null)
            {
                var product = wishlist.Product;

                var existingCart = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.UserId == user.Id && ci.ProductId == product.Id);

                if (existingCart != null)
                {
                    var newQuantity = existingCart.Quantity + 1;
                    existingCart.Quantity = newQuantity > product.Stock ? product.Stock : newQuantity;
                }
                else
                {
                    var quantityToAdd = product.Stock >= 1 ? 1 : 0;
                    var cartItem = new CartItem
                    {
                        UserId = user.Id,
                        ProductId = product.Id,
                        Name = product.Name,
                        Price = product.Price,
                        Quantity = quantityToAdd,
                        Stock = product.Stock
                    };
                    _context.CartItems.Add(cartItem);
                }

                _context.Wishlists.Remove(wishlist);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }


    }
}

