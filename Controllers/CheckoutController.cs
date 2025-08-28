using ABC_Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly CartService _cartService;
        private readonly OrderService _orderService;

        public CheckoutController(CartService cartService, OrderService orderService)
        {
            _cartService = cartService;
            _orderService = orderService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProceedToCheckout()
        {
            var email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Please log in to proceed with checkout.";
                return RedirectToAction("Login", "Customer");
            }

            var cartItems = await _cartService.GetCartAsync(email);
            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction("ViewCart", "CustomerCart");
            }

            double total = (double)cartItems.Sum(item => item.Price * item.Quantity);

            try
            {
                var orderId = await _orderService.PlaceOrderAsync(email, cartItems, total);
                await _cartService.ClearCartAsync(email);

                TempData["SuccessMessage"] = "Order placed successfully!";
                return RedirectToAction("Index", "Home");
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message; // e.g. "Insufficient stock for one or more items."
                return RedirectToAction("ViewCart", "CustomerCart");
            }


        }

    }
}
