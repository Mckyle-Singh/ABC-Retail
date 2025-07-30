using ABC_Retail.Models;
using ABC_Retail.Services;
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
    }
}
