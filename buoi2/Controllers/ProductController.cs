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
        public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository)
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
        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();

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
        public async Task<IActionResult> Add()
        {
            var categories = await _categoryRepository.GetAllAsync();
            if (categories == null || !categories.Any())
            {
                ViewBag.Categories = new SelectList(Enumerable.Empty<SelectListItem>(), "Id", "Name");
            }
            else
            {
                ViewBag.Categories = new SelectList(categories, "Id", "Name");
            }
            return View();
        }

        // Xử lý thêm sản phẩm mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Product product, IFormFile imageUrl)
        {
            if (ModelState.IsValid)
            {
                if (imageUrl != null && imageUrl.Length > 0)
                {
                    product.ImageUrl = await SaveImage(imageUrl);
                }
                await _productRepository.AddAsync(product);
                return RedirectToAction(nameof(Index));
            }

            // Nếu ModelState không hợp lệ, kiểm tra danh sách categories
            var categories = await _categoryRepository.GetAllAsync();
            if (categories == null || !categories.Any())
            {
                ViewBag.Categories = new SelectList(Enumerable.Empty<SelectListItem>(), "Id", "Name");
            }
            else
            {
                ViewBag.Categories = new SelectList(categories, "Id", "Name");
            }
            return View(product);
        }

        
        [Authorize(Roles = "Admin")]
        private async Task<string> SaveImage(IFormFile image)
        {
            // Ensure the images directory exists
            var imagesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
            if (!Directory.Exists(imagesDir))
            {
                Directory.CreateDirectory(imagesDir);
            }

            // Generate a unique filename to avoid conflicts
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
            var savePath = Path.Combine(imagesDir, fileName);

            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            return "/images/" + fileName;
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
        public async Task<IActionResult> Update(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var categories = await _categoryRepository.GetAllAsync();
            if (categories == null || !categories.Any())
            {
                ViewBag.Categories = new SelectList(Enumerable.Empty<SelectListItem>(), "Id", "Name");
            }
            else
            {
                ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            }
            return View(product);
        }

        // Xử lý cập nhật sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, Product product, IFormFile? imageUrl)
        {
            ModelState.Remove("ImageUrl"); // Remove ImageUrl validation if not uploading a new image
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingProduct = await _productRepository.GetByIdAsync(id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                if (imageUrl != null && imageUrl.Length > 0)
                {
                    // Delete old image if it exists
                    if (!string.IsNullOrEmpty(existingProduct.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingProduct.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }
                    // Save new image
                    existingProduct.ImageUrl = await SaveImage(imageUrl);
                }

                // Update product details
                existingProduct.Name = product.Name;
                existingProduct.Price = product.Price;
                existingProduct.Description = product.Description;
                existingProduct.CategoryId = product.CategoryId;

                await _productRepository.UpdateAsync(existingProduct);
                return RedirectToAction(nameof(Index));
            }

            // Nếu ModelState không hợp lệ, kiểm tra danh sách categories
            var categories = await _categoryRepository.GetAllAsync();
            if (categories == null || !categories.Any())
            {
                ViewBag.Categories = new SelectList(Enumerable.Empty<SelectListItem>(), "Id", "Name");
            }
            else
            {
                ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            }
            return View(product);
        }

        // Hiển thị form xác nhận xóa sản phẩm
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Delete associated image if it exists
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", product.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

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