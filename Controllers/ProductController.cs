using ABC_Retail.Models;
using ABC_Retail.Services;
using Azure;
using DotNetEnv;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
    public class ProductController : Controller
    {
        private readonly ProductService _productService;

        public ProductController(ProductService productService)
        {
            _productService = productService;
        }

        public async Task<IActionResult> Seed()
        {
            var product = new Product
            {
                RowKey = Guid.NewGuid().ToString(),
                Name = "Gaming Headset",
                Category = "Electronics",
                Price = 999.99,
                StockQty = 25,
                ImageUrl = "https://via.placeholder.com/150",
                Description = "Surround-sound gaming headset with noise-canceling mic"
            };

            await _productService.AddProductAsync(product);
            return RedirectToAction("Index");
        }

        // GET: Product/Create
        public IActionResult Create()
        {
            var product = new Product
            {
                RowKey = Guid.NewGuid().ToString(),
                PartitionKey = "Retail"
            };
            return View(product);
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            product.PartitionKey = "Retail";
            product.RowKey = Guid.NewGuid().ToString(); // unique SKU

            // 👉 Log the value from the form submission
            Console.WriteLine($"Incoming RowKey: {product.RowKey}");


            if (!ModelState.IsValid)
            {
                // Log validation errors to console (or to a logger)
                foreach (var entry in ModelState)
                {
                    foreach (var error in entry.Value.Errors)
                    {
                        Console.WriteLine($"{entry.Key}: {error.ErrorMessage}");
                    }
                }

                return View(product);
            }


            await _productService.AddProductAsync(product);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetProductsAsync();
            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string rowKey)
        {
            var product = await _productService.GetProductAsync(rowKey);
            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product)
        {
            // Restore ETag from raw form post
            product.ETag = new ETag(Request.Form["ETag"]);

            if (!ModelState.IsValid)
            {
                return View(product);
            }

            try
            {
                var existing = await _productService.GetProductAsync(product.RowKey);
                if (existing == null) return NotFound();

                // Apply updates
                existing.Name = product.Name;
                existing.Category = product.Category;
                existing.Price = product.Price;
                existing.StockQty = product.StockQty;
                existing.ImageUrl = product.ImageUrl;
                existing.Description = product.Description;

                await _productService.UpdateProductAsync(existing);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update error: {ex.Message}");
                ModelState.AddModelError("", "Error updating product.");
                return View(product);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string rowKey)
        {
            try
            {
                await _productService.DeleteProductAsync(rowKey);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Delete error: {ex.Message}");
                // Optional: add error message to UI
                TempData["Error"] = "Failed to delete product.";
                return RedirectToAction("Index");
            }
        }

    }
}
