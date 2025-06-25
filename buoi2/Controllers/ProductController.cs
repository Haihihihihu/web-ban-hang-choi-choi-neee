using buoi2.Models;
using buoi2.Repositories;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using buoi2.Areas.Admin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace buoi2.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ProductController(IProductRepository productRepository,
       ICategoryRepository categoryRepository, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _userManager = userManager;
            _context = context;
        }
        // Hiển thị danh sách sản phẩm

        public async Task<IActionResult> Index(string searchName, string sortBy, int? categoryId)
        {
            var products = await _productRepository.GetAllAsync(searchName, sortBy, categoryId);

            // Lưu các tham số tìm kiếm và sắp xếp để hiển thị lại trên View
            ViewBag.SearchName = searchName;
            ViewBag.SortBy = sortBy;
            ViewBag.CategoryId = categoryId;

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);

            var productIds = products.Select(p => p.Id).ToList();
            var reviews = await _context.ProductReviews
                                        .Where(r => productIds.Contains(r.ProductId))
                                        .ToListAsync();

            foreach (var product in products)
            {
                var productReviews = reviews.Where(r => r.ProductId == product.Id).ToList();
                if (productReviews.Any())
                {
                    product.AverageRating = productReviews.Average(r => r.Rating);
                    product.ReviewCount = productReviews.Count;
                }
            }
            return View(products);
        }
        // Hiển thị form thêm sản phẩm mới
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add()
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }
        // Xử lý thêm sản phẩm mới
        [HttpPost]
        public async Task<IActionResult> Add(Product product, IFormFile
       imageUrl)
        {
            if (ModelState.IsValid)
            {
                if (imageUrl != null)
                {
                    // Lưu hình ảnh đại diện tham khảo bài 02 hàm SaveImage
                    product.ImageUrl = await SaveImage(imageUrl);
                }
                await _productRepository.AddAsync(product);
                return RedirectToAction(nameof(Index));
            }
            // Nếu ModelState không hợp lệ, hiển thị form với dữ liệu đã nhập
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(product);
        }
        
        [Authorize(Roles = "Admin")]
        private async Task<string> SaveImage(IFormFile image)
        {
            //Thay đổi đường dẫn theo cấu hình của bạn
            var savePath = Path.Combine("wwwroot/images", image.FileName);
            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }
            return "/images/" + image.FileName; // Trả về đường dẫn tương đối
        }
        // Hiển thị thông tin chi tiết sản phẩm
        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

            var reviews = await _productRepository.GetReviewsByProductIdAsync(id); // lấy danh sách review
            ViewBag.Reviews = reviews;
            if (reviews.Any())
            {
                var averageRating = reviews.Average(r => r.Rating);    // trung bình sao
                var reviewCount = reviews.Count();
                ViewBag.AverageRating = averageRating;
                ViewBag.ReviewCount = reviewCount;
            }
            else
            {
                ViewBag.AverageRating = null;
                ViewBag.ReviewCount = 0;
            }


            return View(product);
        }

        //Thêm review cho sản phẩm
        [HttpPost]
        [Authorize] // Bắt buộc đăng nhập
        public async Task<IActionResult> AddReview(int productId, int rating, string comment)
        {
            var user = await _userManager.GetUserAsync(User); // lấy user hiện tại
            var review = new ProductReview
            {
                ProductId = productId,
                Rating = rating,
                Comment = comment,
                UserId = user.Id,             // <-- Gán UserId
                UserName = user.UserName,     // <-- Gán UserName
                CreatedAt = DateTime.Now
            };
            await _productRepository.AddReviewAsync(review);
            return RedirectToAction(nameof(Display), new { id = productId });
        }

        // Hiển thị form cập nhật sản phẩm
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name",
           product.CategoryId);
            return View(product);
        }
        // Xử lý cập nhật sản phẩm
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, Product product,
       IFormFile imageUrl)
        {
            ModelState.Remove("ImageUrl"); // Loại bỏ xác thực ModelState cho ImageUrl
            if (id != product.Id)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                var existingProduct = await
               _productRepository.GetByIdAsync(id); // Giả định có phương thức GetByIdAsync
                                                    // Giữ nguyên thông tin hình ảnh nếu không có hình mới được tải lên
                if (imageUrl == null)
                {
                    product.ImageUrl = existingProduct.ImageUrl;
                }
                else
                {
                    // Lưu hình ảnh mới
                    product.ImageUrl = await SaveImage(imageUrl);
                }
                // Cập nhật các thông tin khác của sản phẩm
                existingProduct.Name = product.Name;
                existingProduct.Price = product.Price;
                existingProduct.Description = product.Description;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.ImageUrl = product.ImageUrl;
                await _productRepository.UpdateAsync(existingProduct);

                return RedirectToAction(nameof(Index));
            }
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(product);
        }
        // Hiển thị form xác nhận xóa sản phẩm
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }
        // Xử lý xóa sản phẩm
        [HttpPost, ActionName("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _productRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteReview(int id)
        {
            // lấy review đơn lẻ, không phải danh sách
            var review = await _productRepository.GetReviewByIdAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            // chỉ chủ nhân comment này mới được xóa
            var currentUserName = User.Identity.Name;
            if (review.UserName != currentUserName)
            {
                return Forbid();
            }

            // xóa review
            await _productRepository.DeleteReviewAsync(id);
            return RedirectToAction("Display", new { id = review.ProductId });
        }


    }
}