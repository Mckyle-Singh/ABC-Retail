using ABC_Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
    public class CustomerCartController : Controller
    {
        private readonly CartService _cartService;
        private readonly ProductService _productService;

        public CustomerCartController(CartService cartService, ProductService productService)
        {
            _cartService = cartService;
            _productService = productService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(string productId, int quantity)
        {
            var product = await _productService.GetProductAsync(productId);
            if (product == null) return NotFound();
            var email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Please log in to use the cart.";
                return RedirectToAction("Login", "Customer");
            }

            await _cartService.AddToCartAsync( product, quantity,email);
            TempData["Message"] = $"{product.Name} added to cart!";
            return RedirectToAction("Index", "Product");
        }

    }
}
