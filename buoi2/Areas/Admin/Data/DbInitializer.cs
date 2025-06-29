using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using buoi2.Models;
using System;
using System.Linq;

namespace buoi2.Data
{
    public static class DbInitializer
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                context.Database.Migrate();

                if (!context.Vouchers.Any())
                {
                    var voucher1 = new Voucher
                    {
                        Code = "GIAM10K",
                        DiscountAmount = 10000,
                        MinOrderAmount = 0,
                        IsForNewUser = false,
                        ExpiryDate = DateTime.Now.AddMonths(1),
                        IsActive = true,
                        UsageCount = 0,
                        MaxUsageCount = 500
                    };

                    var voucher2 = new Voucher
                    {
                        Code = "NEWUSER50",
                        DiscountPercent = 50,
                        MinOrderAmount = 0,
                        IsForNewUser = true,
                        ExpiryDate = DateTime.Now.AddMonths(3),
                        IsActive = true,
                        UsageCount = 0,
                        MaxUsageCount = 200
                    };

                    context.Vouchers.AddRange(voucher1, voucher2);
                    context.SaveChanges();
                }
            }
        }
    }
}
