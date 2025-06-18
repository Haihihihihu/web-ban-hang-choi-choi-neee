using buoi2.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace buoi2.Repositories
{

    public class EFProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public EFProductRepository(ApplicationDbContext context) {
            _context = context;   
        }
        public IEnumerable<Product> GetProducts(string searchName, string sortBy)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(p => p.Name.ToLower().Contains(searchName.ToLower()));
            }

            if (sortBy == "price_asc")
            {
                query = query.OrderBy(p => p.Price);
            }
            else if (sortBy == "price_desc")
            {
                query = query.OrderByDescending(p => p.Price);
            }

            return query.ToList();
        }
        public async Task<IEnumerable<Product>> GetAllAsync(string searchName, string sortBy)
        {
            // return await _context.Products.ToListAsync();
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(p => p.Name.ToLower().Contains(searchName.ToLower()));
            }

            if (sortBy == "price_asc")
            {
                query = query.OrderBy(p => p.Price);
            }
            else if (sortBy == "price_desc")
            {
                query = query.OrderByDescending(p => p.Price);
            }

            return await query.ToListAsync();
        }
        public async Task<Product> GetByIdAsync(int id)
        {
            // return await _context.Products.FindAsync(id);
            // lấy thông tin kèm theo category
            return await _context.Products.Include(p =>
           p.Category).FirstOrDefaultAsync(p => p.Id == id);
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
            var product = await _context.Products.FindAsync(id);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
    }
}

