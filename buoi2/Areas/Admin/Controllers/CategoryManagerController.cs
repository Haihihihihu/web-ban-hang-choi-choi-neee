using buoi2.Areas.Admin.Models;
using buoi2.Models;
using buoi2.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace buoi2.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryManagerController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IWebHostEnvironment _env;
        
        public CategoryManagerController(ICategoryRepository categoryRepository, IWebHostEnvironment env)
        {
            _categoryRepository = categoryRepository;
            _env = env;
        }

        // Hiển thị danh sách danh mục
        public async Task<IActionResult> Index()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return View(categories);
        }

        // Hiển thị form thêm danh mục
        public IActionResult Add()
        {
            return View();
        }

        // Xử lý thêm danh mục
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Category category, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra trùng tên
                    var existingCategories = await _categoryRepository.GetAllAsync();
                    if (existingCategories.Any(c => c.Name.ToLower() == category.Name.ToLower()))
                    {
                        ModelState.AddModelError("Name", "Danh mục này đã tồn tại!");
                        return View(category);
                    }

                    // Xử lý upload file ảnh
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var uploads = Path.Combine(_env.WebRootPath, "images");
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                        var filePath = Path.Combine(uploads, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }
                        category.ImageUrl = "/images/" + fileName;
                    }

                    await _categoryRepository.AddAsync(category);
                    TempData["Success"] = "Thêm danh mục thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi thêm danh mục: " + ex.Message);
                }
            }
            return View(category);
        }

        // Hiển thị form chỉnh sửa danh mục
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // Xử lý chỉnh sửa danh mục
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category, IFormFile imageFile)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra trùng tên (trừ chính nó)
                    var existingCategories = await _categoryRepository.GetAllAsync();
                    if (existingCategories.Any(c => c.Id != id && c.Name.ToLower() == category.Name.ToLower()))
                    {
                        ModelState.AddModelError("Name", "Danh mục này đã tồn tại!");
                        return View(category);
                    }

                    // Xử lý upload file ảnh mới nếu có
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var uploads = Path.Combine(_env.WebRootPath, "images");
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                        var filePath = Path.Combine(uploads, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }
                        category.ImageUrl = "/images/" + fileName;
                    }
                    // Nếu không upload file mới, giữ nguyên ảnh cũ (ImageUrl đã được truyền qua input hidden)

                    await _categoryRepository.UpdateAsync(category);
                    TempData["Success"] = "Cập nhật danh mục thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật danh mục: " + ex.Message);
                }
            }
            return View(category);
        }

        // Xử lý xóa danh mục
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(id);
                if (category == null)
                {
                    return NotFound();
                }

                // Kiểm tra xem danh mục có sản phẩm nào không
                if (category.Products.Any())
                {
                    TempData["Error"] = "Không thể xóa danh mục đang có sản phẩm!";
                    return RedirectToAction(nameof(Index));
                }

                await _categoryRepository.DeleteAsync(id);
                TempData["Success"] = "Xóa danh mục thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi xóa danh mục: " + ex.Message;
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
} 