using ABC_Retail.Models;
using ABC_Retail.Models.DTOs;
using ABC_Retail.Models.ViewModels;
using ABC_Retail.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ABC_Retail.Controllers
{
    public class AdminController : Controller
    {
        private readonly AdminService _adminService;
        private readonly ProductService _productService;
        private readonly BlobImageService _blobImageService;
        private readonly ImageUploadQueueService _queueService;
        private readonly CustomerService _customerService;
        private readonly OrderService _orderService;



        public AdminController(AdminService adminService, ProductService productService, BlobImageService blobImageService, ImageUploadQueueService queueService, CustomerService customerService, OrderService orderService)
        {
            _adminService = adminService;
            _productService = productService;
            _blobImageService = blobImageService;
            _queueService = queueService;
            _customerService = customerService;
            _orderService = orderService;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product product)
        {
            product.PartitionKey = "Retail";
            product.RowKey = Guid.NewGuid().ToString(); // Unique SKU

            Console.WriteLine($"Incoming RowKey: {product.RowKey}");

            if (!ModelState.IsValid)
            {
                foreach (var entry in ModelState)
                {
                    foreach (var error in entry.Value.Errors)
                    {
                        Console.WriteLine($"{entry.Key}: {error.ErrorMessage}");
                    }
                }

                return View(product);
            }

            string? originalFileName = null;

            // ✅ Upload image to Blob Storage
            if (product.ImageFile?.Length > 0)
            {
                using var stream = product.ImageFile.OpenReadStream();
                var contentType = product.ImageFile.ContentType;
                originalFileName = product.ImageFile.FileName;

                try
                {
                    product.ImageUrl = await _blobImageService.UploadImageAsync(stream, originalFileName, contentType);
                    Console.WriteLine($"Image uploaded: {product.ImageUrl}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Image upload failed: {ex.Message}");
                    ModelState.AddModelError("ImageFile", "Image upload failed. Please try again.");
                    return View(product);
                }
            }

            // ✅ Enqueue image processing
            if (!string.IsNullOrWhiteSpace(product.ImageUrl))
            {
                var message = new ImageUploadQueueMessageDto
                {
                    BlobUrl = product.ImageUrl,
                    FileName = originalFileName,
                    UploadedByUserId = User.Identity?.Name ?? "system",
                    UploadedAt = DateTime.UtcNow,
                    ProductId = product.RowKey
                };

                try
                {
                    await _queueService.EnqueueImageUploadAsync(message);
                    Console.WriteLine($"Image upload message enqueued for product {product.RowKey}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to enqueue image upload: {ex.Message}");
                    // Optional: log but don’t block creation
                }
            }

            await _productService.AddProductAsync(product);
            TempData["SuccessMessage"] = "✅ Product created successfully.";
            return RedirectToAction("ManageProducts");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(string rowKey)
        {
            try
            {
                await _productService.DeleteProductAsync(rowKey);
                TempData["SuccessMessage"] = "🗑️ Product deleted successfully.";
                return RedirectToAction("ManageProducts");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Delete error: {ex.Message}");
                TempData["ErrorMessage"] = "❌ Failed to delete product. Please try again.";
                return RedirectToAction("ManageProducts");
            }
        }

        public async Task<IActionResult> ViewCustomers()
        {
            var customers = await _customerService.GetActiveCustomersAsync();
            return View(customers);
        }

        public async Task<IActionResult> ViewAllOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            var first = orders.FirstOrDefault();
            Console.WriteLine($"Name: {first?.CustomerName}, Total: {first?.TotalAmount}, Email: {first?.Email}");
            return View(orders);
        }




        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Clear all session data
            TempData["SuccessMessage"] = "You have been logged out.";
            return RedirectToAction("Login", "Admin");
        }
    }
}
