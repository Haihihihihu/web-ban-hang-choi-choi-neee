using buoi2.Models;

namespace buoi2.ViewModels
{
    public class HomeViewModel
    {
        public IEnumerable<Product> FeaturedProducts { get; set; } = new List<Product>();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public IEnumerable<Product> AllProducts { get; set; } = new List<Product>();
    }
}
