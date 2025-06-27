using buoi2.Areas.Admin.Models;
using buoi2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace buoi2.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserManagerController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserManagerController> _logger;

        public UserManagerController(UserManager<ApplicationUser> userManager, ILogger<UserManagerController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        // Hiển thị danh sách tất cả user
        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users");
                TempData["Error"] = "Error loading users.";
                return View(new List<ApplicationUser>());
            }
        }

        // Xoá user theo id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            _logger.LogInformation("DeleteUser called with id={Id}", id);

            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Invalid user ID.";
                return RedirectToAction("Index");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User {Id} not found.", id);
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == user.Id)
            {
                _logger.LogWarning("User {Id} cannot delete themselves.", id);
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction("Index");
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} deleted successfully.", user.Email);
                TempData["Success"] = $"User {user.Email} deleted successfully!";
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Error deleting user {Email}: {Errors}", user.Email, errors);
                TempData["Error"] = $"Error deleting user: {errors}";
            }

            return RedirectToAction("Index");
        }


        // Action để lấy thông tin user (cho debugging)
        [HttpGet]
        public async Task<IActionResult> GetUserInfo(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            return Json(new
            {
                success = true,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    fullName = user.FullName
                }
            });
        }
    }
}