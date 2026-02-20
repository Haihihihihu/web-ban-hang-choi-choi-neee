using buoi2.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace buoi2.Repositories
{
    public class EFProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;
        public EFProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllAsync(string searchName = null, string sortBy = null, int? categoryId = null)
        {
            var query = _context.Products.AsQueryable();

            // Lọc theo tên
            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(p => p.Name.Contains(searchName));
            }

            // Lọc theo danh mục
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // Sắp xếp
            switch (sortBy)
            {
                case "price_asc":
                    query = query.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(p => p.Price);
                    break;
            }

            return await query.ToListAsync();
        }

        public async Task<Product> GetByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task AddAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var product = await GetByIdAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<ProductReview>> GetReviewsByProductIdAsync(int productId)
        {
            return await _context.ProductReviews
                                 .Where(r => r.ProductId == productId)
                                 .OrderByDescending(r => r.CreatedAt)
                                 .ToListAsync();    
        }

        public async Task AddReviewAsync(ProductReview review)
        {
            _context.ProductReviews.Add(review);
            await _context.SaveChangesAsync();
        }
        public async Task<ProductReview> GetReviewByIdAsync(int id)
        {
            return await _context.ProductReviews.FindAsync(id);
        }
        public async Task DeleteReviewAsync(int id)
        {
            var review = await _context.ProductReviews.FindAsync(id);
            if (review != null)
            {
                _context.ProductReviews.Remove(review);
                await _context.SaveChangesAsync();
            }
        }
    }
}
