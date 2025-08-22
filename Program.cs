using ABC_Retail.Services;
using ABC_Retail.Services.Logging.Core;
using ABC_Retail.Services.Logging.Domains.Products;
using ABC_Retail.Services.Logging.File_Logging;
using ABC_Retail.Services.Messaging;
using ABC_Retail.Services.Queues;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using DotNetEnv;

namespace ABC_Retail
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout
                options.Cookie.HttpOnly = true;                 // Secure the session cookie
                options.Cookie.IsEssential = true;              // Ensure it's saved even if GDPR applies
            });

            // Load secrets from .env file (only for local dev)
            Env.Load();

            // Load environment variable securely
            string? connectionString = Environment.GetEnvironmentVariable("AzureStorageConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception("AzureStorageConnection environment variable not found.");
            }

            // Set UNC path for centralized logging (Azure File Share)
            Environment.SetEnvironmentVariable("LogBasePath", @"\\st10118454.file.core.windows.net\product-logs");


            // Register BlobServiceClient for DI
            builder.Services.AddSingleton(new BlobServiceClient(connectionString));

            // Instantiate TableServiceClient and register Services
            TableServiceClient tableServiceClient = new TableServiceClient(connectionString);
            builder.Services.AddSingleton<TableServiceClient>(tableServiceClient);

            //Register Queue Services
            builder.Services.AddSingleton(new ImageUploadQueueService(connectionString, "image-upload-queue"));
            builder.Services.AddSingleton(new OrderPlacedQueueService(connectionString, "order-placed-queue"));
            builder.Services.AddSingleton(new ProductQueueService(connectionString, "product-updates-queue"));

            // Register QueueReaderService and its QueueClient
            builder.Services.AddSingleton<IQueueReaderService, QueueReaderService>();
            builder.Services.AddSingleton(sp =>
            {
                const string queueName = "product-updates-queue";
                return new QueueClient(connectionString, queueName);
            });


            // Register Core Domain Services
            builder.Services.AddSingleton(sp =>
            {
                var tableClient = sp.GetRequiredService<TableServiceClient>();
                var productQueue = sp.GetRequiredService<ProductQueueService>();
                return new ProductService(tableClient, productQueue);
            });

            builder.Services.AddSingleton(new CustomerService(tableServiceClient));
            builder.Services.AddSingleton(new CartService(tableServiceClient));
            builder.Services.AddSingleton(new AdminService(tableServiceClient));
            builder.Services.AddScoped<BlobImageService>();

            // Register OrderService with both dependencies
            builder.Services.AddSingleton(sp =>
            {
                var orderQueueService = sp.GetRequiredService<OrderPlacedQueueService>();
                return new OrderService(tableServiceClient, orderQueueService);
            });

            // Register Logging Infrastructure
            builder.Services.AddSingleton<ILogPathResolver>(sp =>
            {
                var logBasePath = Environment.GetEnvironmentVariable("LogBasePath");
                return new FileLogPathResolver(logBasePath);
            });


            builder.Services.AddSingleton<ILogWriter, FileLogWriter>();
            builder.Services.AddScoped<ProductLogService>();



            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseSession();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
