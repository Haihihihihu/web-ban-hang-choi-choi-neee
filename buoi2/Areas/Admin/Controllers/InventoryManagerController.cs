using buoi2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace buoi2.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class InventoryManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventoryManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/InventoryManager
        public async Task<IActionResult> Index()
        {
            _context.ChangeTracker.Clear();

            var products = await _context.Products
                .AsNoTracking()
                .OrderBy(p => p.Id)
                .ToListAsync();

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return View(products);
        }

        // GET: Admin/InventoryManager/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        // POST: Admin/InventoryManager/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Stock")] Product product)
        {
            if (id != product.Id)
                return NotFound();

            try
            {
                // Sử dụng ExecuteSqlRaw để update trực tiếp
                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE Products SET Stock = {0} WHERE Id = {1}",
                    product.Stock, id);

                if (rowsAffected > 0)
                {
                    TempData["Message"] = $"Cập nhật tồn kho thành công! Số lượng mới: {product.Stock}";
                }
                else
                {
                    TempData["Message"] = "Không tìm thấy sản phẩm hoặc không có thay đổi nào.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Có lỗi xảy ra: " + ex.Message;

                // Load lại product data cho view
                var productInDb = await _context.Products.FindAsync(id);
                if (productInDb == null)
                    return NotFound();

                return View(productInDb);
            }
        }


        private bool ProductExists(int id)
        {
            return _context.Products.AsNoTracking().Any(e => e.Id == id);
        }
    }
}