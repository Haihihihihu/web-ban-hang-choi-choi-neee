using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using buoi2.Models;


namespace buoi2.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserManagerController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Admin/UserManager
        public async Task<IActionResult> Index()
        {
            // Check if it's an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                Request.ContentType?.Contains("application/json") == true ||
                Request.Query.ContainsKey("ajax"))
            {
                try
                {
                    var users = await _context.Database.SqlQueryRaw<ApplicationUser>(
                        "SELECT * FROM AspNetUsers ORDER BY Email").ToListAsync();

                    return Json(new { success = true, data = users });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }

            // Return the view for normal page load
            return View();
        }

        // GET: Admin/UserManager/GetUser/5
        public async Task<IActionResult> GetUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "User ID is required" });
            }

            try
            {
                var user = await _context.Database.SqlQueryRaw<ApplicationUser>(
                    "SELECT * FROM AspNetUsers WHERE Id = {0}", id).FirstOrDefaultAsync();

                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                // Get user roles using raw SQL
                var userRoles = await _context.Database.SqlQueryRaw<string>(
                    @"SELECT r.Name FROM AspNetRoles r 
                      INNER JOIN AspNetUserRoles ur ON r.Id = ur.RoleId 
                      WHERE ur.UserId = {0}", id).ToListAsync();

                // Get all roles
                var allRoles = await _context.Database.SqlQueryRaw<string>(
                    "SELECT Name FROM AspNetRoles ORDER BY Name").ToListAsync();

                return Json(new
                {
                    success = true,
                    user = user,
                    userRoles = userRoles,
                    allRoles = allRoles
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/UserManager/UpdateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUser(string id, string email, List<string> selectedRoles)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "ID and Email are required" });
            }

            try
            {
                // Check if user exists
                var userExists = await _context.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(*) as Value FROM AspNetUsers WHERE Id = {0}", id).FirstAsync();

                if (userExists == 0)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Update user email using raw SQL
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE AspNetUsers SET Email = {0}, UserName = {1}, NormalizedEmail = {2}, NormalizedUserName = {3} WHERE Id = {4}",
                    email, email, email.ToUpper(), email.ToUpper(), id);

                // Remove all existing roles
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM AspNetUserRoles WHERE UserId = {0}", id);

                // Add new roles
                if (selectedRoles != null && selectedRoles.Any())
                {
                    foreach (var roleName in selectedRoles)
                    {
                        var roleId = await _context.Database.SqlQueryRaw<string>(
                            "SELECT Id FROM AspNetRoles WHERE Name = {0}", roleName).FirstOrDefaultAsync();

                        if (!string.IsNullOrEmpty(roleId))
                        {
                            await _context.Database.ExecuteSqlRawAsync(
                                "INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES ({0}, {1})",
                                id, roleId);
                        }
                    }
                }

                return Json(new { success = true, message = "User updated successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/UserManager/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "User ID is required" });
            }

            try
            {
                // Check if user exists
                var userExists = await _context.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(*) as Value FROM AspNetUsers WHERE Id = {0}", id).FirstAsync();

                if (userExists == 0)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Delete in proper order to avoid foreign key constraints
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM AspNetUserRoles WHERE UserId = {0}", id);

                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM AspNetUserClaims WHERE UserId = {0}", id);

                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM AspNetUserLogins WHERE UserId = {0}", id);

                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM AspNetUserTokens WHERE UserId = {0}", id);

                // Finally delete the user
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM AspNetUsers WHERE Id = {0}", id);

                return Json(new { success = true, message = "User deleted successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Admin/UserManager/GetRoles
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var roles = await _context.Database.SqlQueryRaw<string>(
                    "SELECT Name FROM AspNetRoles ORDER BY Name").ToListAsync();

                return Json(new { success = true, data = roles });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/UserManager/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string email, string password, List<string> selectedRoles)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return Json(new { success = false, message = "Email and Password are required" });
            }

            try
            {
                // Check if user already exists
                var existingUser = await _context.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(*) as Value FROM AspNetUsers WHERE Email = {0}", email).FirstAsync();

                if (existingUser > 0)
                {
                    return Json(new { success = false, message = "User with this email already exists" });
                }

                // Generate new user ID
                var userId = Guid.NewGuid().ToString();
                var hasher = new PasswordHasher<ApplicationUser>();
                var hashedPassword = hasher.HashPassword(null, password);

                // Create user using raw SQL
                await _context.Database.ExecuteSqlRawAsync(
                    @"INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, 
                      EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, 
                      TwoFactorEnabled, LockoutEnabled, AccessFailedCount) 
                      VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12})",
                    userId, email, email.ToUpper(), email, email.ToUpper(),
                    true, hashedPassword, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(),
                    false, false, true, 0);

                // Add roles if selected
                if (selectedRoles != null && selectedRoles.Any())
                {
                    foreach (var roleName in selectedRoles)
                    {
                        var roleId = await _context.Database.SqlQueryRaw<string>(
                            "SELECT Id FROM AspNetRoles WHERE Name = {0}", roleName).FirstOrDefaultAsync();

                        if (!string.IsNullOrEmpty(roleId))
                        {
                            await _context.Database.ExecuteSqlRawAsync(
                                "INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES ({0}, {1})",
                                userId, roleId);
                        }
                    }
                }

                return Json(new { success = true, message = "User created successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Admin/UserManager/SearchUsers
        public async Task<IActionResult> SearchUsers(string searchTerm)
        {
            try
            {
                if (string.IsNullOrEmpty(searchTerm))
                {
                    // Return all users if no search term
                    var allUsers = await _context.Database.SqlQueryRaw<ApplicationUser>(
                        "SELECT * FROM AspNetUsers ORDER BY Email").ToListAsync();
                    return Json(new { success = true, data = allUsers });
                }

                var users = await _context.Database.SqlQueryRaw<ApplicationUser>(
                    "SELECT * FROM AspNetUsers WHERE Email LIKE {0} ORDER BY Email",
                    $"%{searchTerm}%").ToListAsync();

                return Json(new { success = true, data = users });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Admin/UserManager/GetUserCount
        public async Task<IActionResult> GetUserCount()
        {
            try
            {
                var count = await _context.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(*) as Value FROM AspNetUsers").FirstAsync();

                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}