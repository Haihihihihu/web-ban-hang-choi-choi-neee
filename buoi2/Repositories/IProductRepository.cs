using buoi2.Models;
using System.Collections.Generic;


namespace buoi2.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync(string searchName = null, string sortBy = null, int? categoryId = null);
        Task<Product> GetByIdAsync(int id);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int id);
    }
    
}
