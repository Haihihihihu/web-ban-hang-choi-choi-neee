using buoi2.Services;
using Microsoft.AspNetCore.Mvc;

namespace buoi2.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class InventoryController : Controller
    {
        private readonly InventoryService _service;
        public InventoryController(InventoryService service) => _service = service;
        public IActionResult Products() => View(_service.GetProducts());
        public IActionResult Orders() => View(_service.GetOrders());

        [HttpPost]
        public IActionResult PlaceOrder(int productId, int quantity)
        {
            var result = _service.PlaceOrder(productId, quantity);
            TempData["Message"] = result;
            return RedirectToAction(nameof(Products));
        }

        [HttpPost]  
        public IActionResult CancelOrder(int orderId)
        {
            var result = _service.CancelOrder(orderId);
            TempData["Message"] = result;
            return RedirectToAction(nameof(Orders));
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
