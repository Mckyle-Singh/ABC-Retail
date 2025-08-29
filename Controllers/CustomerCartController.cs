using ABC_Retail.Models;
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
            TempData["SuccessMessage"] = $"{product.Name} added to cart!";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(string productRowKey, int newQty)
        {
            var email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Please log in to update your cart.";
                return RedirectToAction("Login", "Customer");
            }

            try
            {
                await _cartService.UpdateQuantityAsync(productRowKey, newQty, email);
                TempData["SuccessMessage"] = "Cart updated successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Something went wrong while updating your cart.";
            }

            return RedirectToAction("ViewCart");

        }


        [HttpGet]
        public async Task<IActionResult> ViewCart()
        {
            var email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Please log in to view your cart.";
                return RedirectToAction("Login", "Customer");
            }

            var cartItems = await _cartService.GetCartAsync(email);
            // Debug log each item
            foreach (var item in cartItems)
            {
                Console.WriteLine($"CartItem: {item.ProductName}, Quantity = {item.Quantity}, Price = {item.Price}");
            }

            return View(cartItems); 
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(string productRowKey)
        {
            var customerEmail = HttpContext.Session.GetString("CustomerEmail");
            await _cartService.RemoveFromCartAsync(productRowKey, customerEmail);
            return RedirectToAction("ViewCart");
        }

    }
}
