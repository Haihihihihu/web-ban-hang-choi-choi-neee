using buoi2.Models;
using buoi2.Repositories;
using buoi2.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace buoi2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public HomeController(
            ILogger<HomeController> logger,
            IProductRepository productRepository,
            ICategoryRepository categoryRepository)
        {
            _logger = logger;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            var categories = await _categoryRepository.GetAllAsync();

            var model = new HomeViewModel
            {
                FeaturedProducts = products.Where(p => p.IsFeatured).ToList(),
                Categories = categories
            };

            return View(model);
        }

        public IActionResult Privacy() => View();

        public IActionResult Contact()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
