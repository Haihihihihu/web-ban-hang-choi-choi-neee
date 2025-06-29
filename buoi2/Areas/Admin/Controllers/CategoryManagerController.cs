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
        public CategoryManagerController(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        // Hiển thị form thêm danh mục
        public IActionResult Add()
        {
            return View();
        }

        // Xử lý thêm danh mục
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Category category)
        {
            if (ModelState.IsValid)
            {
                await _categoryRepository.AddAsync(category);
                TempData["Success"] = "Thêm danh mục thành công!";
                return RedirectToAction("Add");
            }
            return View(category);
        }
    }
} 