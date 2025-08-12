using ABC_Retail.Models;
using ABC_Retail.Models.ViewModels;
using ABC_Retail.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace ABC_Retail.Controllers
{
    public class AdminController : Controller
    {
        private readonly AdminService _adminService;
        private readonly ProductService _productService;

        public AdminController(AdminService adminService, ProductService productService)
        {
            _adminService = adminService;
            _productService = productService;
        }

        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            return Convert.ToBase64String(sha.ComputeHash(bytes));
        }
        public async Task<IActionResult> Seed()
        {
            var email = "admin@example.com";
            var plainPassword = "123456";

            var admin = new Admin
            {
                RowKey = email.ToLower(),
                PartitionKey = "Admin",
                FullName = "System Administrator",
                Email = email,
                PasswordHash = HashPassword(plainPassword),
                CreatedOn = DateTime.UtcNow,
                IsActive = true
            };

            await _adminService.AddAdminAsync(admin);
            TempData["Message"] = "✅ Admin seeded successfully.";
            return RedirectToAction("Login");
        }
        public IActionResult Login()
        {
            return View(); 
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginAdminViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var admin = await _adminService.LoginAdminAsync(
                model.Email.ToLower().Trim(), model.Password);

            if (admin == null)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }

            HttpContext.Session.SetString("AdminEmail", admin.Email);
            TempData["SuccessMessage"] = "Welcome, Admin!";
            return RedirectToAction("Dashboard", "Admin");
        }

        public IActionResult Dashboard()
        {
            var email = HttpContext.Session.GetString("AdminEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login","Admin");

            var viewModel = new AdminDashboardViewModel
            {
                AdminEmail = email
                // We'll add metrics later
            };

            return View(viewModel);
        }

        public async Task<IActionResult> ManageProducts()
        {
            var products = await _productService.GetProductsAsync();

            // Intention: Display all products for administrative review and action
            return View("ManageProducts", products);
        }

        // GET: Product/Create
        public IActionResult CreateProduct()
        {
            var product = new Product
            {
                RowKey = Guid.NewGuid().ToString(),
                PartitionKey = "Retail"
            };
            return View(product);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Clear all session data
            TempData["SuccessMessage"] = "You have been logged out.";
            return RedirectToAction("Login", "Admin");
        }
    }
}
