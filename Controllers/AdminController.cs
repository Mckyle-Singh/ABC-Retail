using ABC_Retail.Models;
using ABC_Retail.Models.DTOs;
using ABC_Retail.Models.ViewModels;
using ABC_Retail.Services;
using ABC_Retail.Services.Logging;
using ABC_Retail.Services.Logging.Core;
using ABC_Retail.Services.Logging.Domains.Products;
using ABC_Retail.Services.Queues;
using Azure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Reflection.Metadata;
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
        private readonly ProductLogService _productLogService;
        private readonly ILogReader _logReader;


        public AdminController(AdminService adminService, ProductService productService, 
            BlobImageService blobImageService, ImageUploadQueueService queueService, 
            CustomerService customerService, OrderService orderService , ProductLogService productLogService , ILogReader logReader)
        {
            _adminService = adminService;
            _productService = productService;
            _blobImageService = blobImageService;
            _queueService = queueService;
            _customerService = customerService;
            _orderService = orderService;
            _productLogService = productLogService;
            _logReader = logReader;
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

        public async Task<IActionResult> Dashboard()
        {
            var email = HttpContext.Session.GetString("AdminEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login","Admin");

            var allLines = await _logReader.ReadLinesAsync(LogDomain.Products);

            var logs = allLines
                    .Where(line =>
                        !line.Contains("details updated") &&
                        !line.EndsWith("updated —"))
                    .OrderByDescending(line => LogUtils.ExtractTimestamp(line))
                    .Take(20)
                    .Select(line =>
                    {
                        var timestamp = LogUtils.ExtractTimestamp(line);
                        var formatted = LogUtils.FormatTimestamp(timestamp);
                        var message = line.Split(" - ", 2)[1]; // Everything after the timestamp
                        return $"{formatted} - {message}";
                    })
                    .ToList();

            var viewModel = new AdminDashboardViewModel
            {
                AdminEmail = email,
                ProductChangeFeed = logs
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
            //  Log product creation
            await _productLogService.LogProductAddedAsync(
            product.RowKey,
            product.Name,
            product.Price,
            product.StockQty);



            TempData["SuccessMessage"] = "✅ Product created successfully.";
            return RedirectToAction("ManageProducts");
        }

        // GET: /Product/Edit/{rowKey}
        [HttpGet]
        public async Task<IActionResult> EditProduct(string rowKey)
        {
            var product = await _productService.GetProductAsync(rowKey);
            if (product == null)
                return NotFound();

            return View(product); // Pass to Razor view
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(Product updatedProduct)
        {
            updatedProduct.PartitionKey = "Retail"; // Ensure PartitionKey is set

            if (!ModelState.IsValid)
            {
                // Rehydrate image preview if validation fails
                var existing = await _productService.GetProductAsync(updatedProduct.RowKey);
                updatedProduct.ImageUrl = existing?.ImageUrl;
                updatedProduct.ETag = existing?.ETag ?? default;
                return View(updatedProduct);
            }

            string? originalFileName = null;

            // ✅ Upload new image if provided
            if (updatedProduct.ImageFile?.Length > 0)
            {
                using var stream = updatedProduct.ImageFile.OpenReadStream();
                var contentType = updatedProduct.ImageFile.ContentType;
                originalFileName = updatedProduct.ImageFile.FileName;

                try
                {
                    updatedProduct.ImageUrl = await _blobImageService.UploadImageAsync(stream, originalFileName, contentType);
                    Console.WriteLine($"Image updated: {updatedProduct.ImageUrl}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Image upload failed: {ex.Message}");
                    ModelState.AddModelError("ImageFile", "Image upload failed. Please try again.");
                    return View(updatedProduct);
                }

                // ✅ Enqueue image update message
                var message = new ImageUploadQueueMessageDto
                {
                    BlobUrl = updatedProduct.ImageUrl,
                    FileName = originalFileName,
                    UploadedByUserId = User.Identity?.Name ?? "system",
                    UploadedAt = DateTime.UtcNow,
                    ProductId = updatedProduct.RowKey
                };

                try
                {
                    await _queueService.EnqueueImageUploadAsync(message);
                    Console.WriteLine($"Image update message enqueued for product {updatedProduct.RowKey}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to enqueue image update: {ex.Message}");
                    // Optional: log but don’t block update
                }
            }
            else
            {
                // Preserve existing image if no new file uploaded
                var existing = await _productService.GetProductAsync(updatedProduct.RowKey);
                updatedProduct.ImageUrl = existing?.ImageUrl;
            }

            // ✅ Defensive ETag fallback
            if (updatedProduct.ETag == default)
            {
                var existing = await _productService.GetProductAsync(updatedProduct.RowKey);
                updatedProduct.ETag = existing?.ETag ?? default;
            }

            // ✅ Update product in Azure Table Storage
            var originalProduct = await _productService.GetProductAsync(updatedProduct.RowKey);
            try
            {
                await _productService.UpdateProductAsync(updatedProduct);
                await _productLogService.LogProductUpdatedAsync(
                   updatedProduct.RowKey,
                   updatedProduct.Name,
                   originalProduct.Price,
                   updatedProduct.Price,
                   originalProduct.StockQty,
                   updatedProduct.StockQty);


            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Update failed: {ex.Message}");
                ModelState.AddModelError("", "Failed to update product. Please try again.");
                return View(updatedProduct);
            }

            TempData["SuccessMessage"] = "✅ Product updated successfully.";
            return RedirectToAction("ManageProducts");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(string rowKey)
        {
            try
            {
                var product = await _productService.GetProductAsync(rowKey);

                await _productService.DeleteProductAsync(rowKey);
                // Log the deletion
                await _productLogService.LogProductDeletedAsync(
                    product.RowKey,
                    product.Name,
                    product.Price,
                    product.StockQty
                );


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
