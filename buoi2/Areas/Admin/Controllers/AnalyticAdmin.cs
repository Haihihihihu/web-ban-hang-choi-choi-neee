using buoi2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace buoi2.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // Thay đổi theo role trong project buoi2
    public class AnalyticAdmin : Controller
    {
        private readonly ApplicationDbContext _context;

        public AnalyticAdmin(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetRevenueData(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                // Nếu không có ngày được chọn, mặc định lấy 30 ngày gần nhất
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    endDate = DateTime.Now.Date;
                    startDate = endDate.Value.AddDays(-30);
                }

                // Đảm bảo endDate bao gồm cả ngày cuối
                endDate = endDate.Value.Date.AddDays(1).AddTicks(-1);

                var revenueData = await _context.Orders
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Revenue = g.Sum(o => o.TotalPrice),
                        OrderCount = g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    data = revenueData.Select(x => new
                    {
                        date = x.Date.ToString("yyyy-MM-dd"),
                        dateDisplay = x.Date.ToString("dd/MM/yyyy"),
                        revenue = x.Revenue,
                        orderCount = x.OrderCount
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSummaryStats(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    endDate = DateTime.Now.Date;
                    startDate = endDate.Value.AddDays(-30);
                }

                endDate = endDate.Value.Date.AddDays(1).AddTicks(-1);

                var orders = await _context.Orders
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                    .ToListAsync();

                var totalRevenue = orders.Sum(o => o.TotalPrice);
                var totalOrders = orders.Count;
                var averageOrderValue = totalOrders > 0 ? Math.Round(totalRevenue / totalOrders, 2) : 0;

                return Json(new
                {
                    success = true,
                    totalRevenue = totalRevenue,
                    totalOrders = totalOrders,
                    averageOrderValue = averageOrderValue,
                    startDate = startDate.Value.ToString("dd/MM/yyyy"),
                    endDate = endDate.Value.AddTicks(1).AddDays(-1).ToString("dd/MM/yyyy")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Thống kê sản phẩm bán chạy
        [HttpGet]
        public async Task<IActionResult> GetTopSellingProducts(DateTime? startDate, DateTime? endDate, int top = 10)
        {
            try
            {
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    endDate = DateTime.Now.Date;
                    startDate = endDate.Value.AddDays(-30);
                }

                endDate = endDate.Value.Date.AddDays(1).AddTicks(-1);

                var topProducts = await _context.OrderDetails
                    .Include(od => od.Product)
                    .Include(od => od.Order)
                    .Where(od => od.Order.OrderDate >= startDate && od.Order.OrderDate <= endDate)
                    .GroupBy(od => new { od.Product.Id, od.Product.Name, od.Product.Price })
                    .Select(g => new
                    {
                        ProductId = g.Key.Id,
                        ProductName = g.Key.Name,
                        ProductPrice = g.Key.Price,
                        TotalQuantity = g.Sum(od => od.Quantity),
                        TotalRevenue = g.Sum(od => od.Quantity * od.Price),
                        OrderCount = g.Select(od => od.OrderId).Distinct().Count()
                    })
                    .OrderByDescending(x => x.TotalQuantity)
                    .Take(top)
                    .ToListAsync();

                return Json(new { success = true, data = topProducts });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Thống kê theo danh mục
        [HttpGet]
        public async Task<IActionResult> GetCategoryStats(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    endDate = DateTime.Now.Date;
                    startDate = endDate.Value.AddDays(-30);
                }

                endDate = endDate.Value.Date.AddDays(1).AddTicks(-1);

                var categoryStats = await _context.OrderDetails
                    .Include(od => od.Product)
                    .ThenInclude(p => p.Category)
                    .Include(od => od.Order)
                    .Where(od => od.Order.OrderDate >= startDate && od.Order.OrderDate <= endDate)
                    .GroupBy(od => new { od.Product.Category.Id, od.Product.Category.Name })
                    .Select(g => new
                    {
                        CategoryId = g.Key.Id,
                        CategoryName = g.Key.Name,
                        TotalQuantity = g.Sum(od => od.Quantity),
                        TotalRevenue = g.Sum(od => od.Quantity * od.Price),
                        ProductCount = g.Select(od => od.ProductId).Distinct().Count(),
                        OrderCount = g.Select(od => od.OrderId).Distinct().Count()
                    })
                    .OrderByDescending(x => x.TotalRevenue)
                    .ToListAsync();

                return Json(new { success = true, data = categoryStats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Thống kê khách hàng
        [HttpGet]
        public async Task<IActionResult> GetCustomerStats(DateTime? startDate, DateTime? endDate, int top = 10)
        {
            try
            {
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    endDate = DateTime.Now.Date;
                    startDate = endDate.Value.AddDays(-30);
                }

                endDate = endDate.Value.Date.AddDays(1).AddTicks(-1);

                var customerStats = await _context.Orders
                    .Include(o => o.ApplicationUser)
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                    .GroupBy(o => new { o.UserId, o.ApplicationUser.UserName, o.ApplicationUser.Email })
                    .Select(g => new
                    {
                        UserId = g.Key.UserId,
                        UserName = g.Key.UserName,
                        Email = g.Key.Email,
                        TotalOrders = g.Count(),
                        TotalSpent = g.Sum(o => o.TotalPrice),
                        AverageOrderValue = g.Average(o => o.TotalPrice),
                        LastOrderDate = g.Max(o => o.OrderDate)
                    })
                    .OrderByDescending(x => x.TotalSpent)
                    .Take(top)
                    .ToListAsync();

                return Json(new { success = true, data = customerStats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Thống kê thanh toán
        [HttpGet]
        public async Task<IActionResult> GetPaymentStats(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    endDate = DateTime.Now.Date;
                    startDate = endDate.Value.AddDays(-30);
                }

                endDate = endDate.Value.Date.AddDays(1).AddTicks(-1);

                var paymentStatsRaw = await _context.Orders
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                    .GroupBy(o => o.PaymentMethod)
                    .Select(g => new
                    {
                        PaymentMethod = g.Key,
                        OrderCount = g.Count(),
                        TotalAmount = g.Sum(o => o.TotalPrice)
                    })
                    .ToListAsync();

                var totalOrders = paymentStatsRaw.Sum(x => x.OrderCount);

                var paymentStats = paymentStatsRaw.Select(x => new
                {
                    x.PaymentMethod,
                    x.OrderCount,
                    x.TotalAmount,
                    Percentage = totalOrders > 0 ? Math.Round((double)x.OrderCount / totalOrders * 100, 2) : 0
                });

                return Json(new { success = true, data = paymentStats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        // Dashboard tổng quan
        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var today = DateTime.Now.Date;
                var thisMonth = new DateTime(today.Year, today.Month, 1);
                var lastMonth = thisMonth.AddMonths(-1);

                // Thống kê hôm nay
                var todayStats = await _context.Orders
                    .Where(o => o.OrderDate.Date == today)
                    .ToListAsync();

                // Thống kê tháng này
                var thisMonthStats = await _context.Orders
                    .Where(o => o.OrderDate >= thisMonth)
                    .ToListAsync();

                // Thống kê tháng trước
                var lastMonthStats = await _context.Orders
                    .Where(o => o.OrderDate >= lastMonth && o.OrderDate < thisMonth)
                    .ToListAsync();

                // Tổng số sản phẩm
                var totalProducts = await _context.Products.CountAsync();

                // Tổng số danh mục
                var totalCategories = await _context.Categories.CountAsync();

                // Tổng số khách hàng
                var totalCustomers = await _context.Users.CountAsync();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        today = new
                        {
                            orders = todayStats.Count,
                            revenue = todayStats.Sum(o => o.TotalPrice)
                        },
                        thisMonth = new
                        {
                            orders = thisMonthStats.Count,
                            revenue = thisMonthStats.Sum(o => o.TotalPrice)
                        },
                        lastMonth = new
                        {
                            orders = lastMonthStats.Count,
                            revenue = lastMonthStats.Sum(o => o.TotalPrice)
                        },
                        totals = new
                        {
                            products = totalProducts,
                            categories = totalCategories,
                            customers = totalCustomers
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}

