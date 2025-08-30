using ABC_Retail.Services;
using ABC_Retail.Services.Logging.AzureFileShare_Logging;
using ABC_Retail.Services.Logging.Core;
using ABC_Retail.Services.Logging.Domains.Orders;
using ABC_Retail.Services.Logging.Domains.Products;
using ABC_Retail.Services.Logging.File_Logging;
using ABC_Retail.Services.Queues;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using DotNetEnv;

namespace ABC_Retail
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ------- Core ASP.NET Setup -------
            builder.Services.AddControllersWithViews();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;                 
                options.Cookie.IsEssential = true;              
            });

            // Load secrets from .env file (only for local dev)
            Env.Load();

            // -------- Storage Connection -------

            string? connectionString = Environment.GetEnvironmentVariable("AzureStorageConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception("AzureStorageConnection environment variable not found.");
            }

            // Register BlobServiceClient for DI
            builder.Services.AddSingleton(new BlobServiceClient(connectionString));

            // Instantiate TableServiceClient and register Services
            TableServiceClient tableServiceClient = new TableServiceClient(connectionString);
            builder.Services.AddSingleton<TableServiceClient>(tableServiceClient);

            //Register Queue Services
            builder.Services.AddSingleton(new ImageUploadQueueService(connectionString, "image-upload-queue"));
            builder.Services.AddSingleton(new OrderPlacedQueueService(connectionString, "order-placed-queue"));
            builder.Services.AddSingleton(new ProductQueueService(connectionString, "product-updates-queue"));
            builder.Services.AddSingleton(new StockReminderQueueService(connectionString, "stock-reminder-queue"));

            // Register Core Domain Services
            builder.Services.AddSingleton(sp =>
            {
                var tableClient = sp.GetRequiredService<TableServiceClient>();
                var productQueue = sp.GetRequiredService<ProductQueueService>();
                return new ProductService(tableClient, productQueue);
            });
            builder.Services.AddSingleton(new CustomerService(tableServiceClient));
            builder.Services.AddSingleton<CartService>(sp =>
            {
                var tableClient = sp.GetRequiredService<TableServiceClient>();
                var productService = sp.GetRequiredService<ProductService>();
                return new CartService(tableClient, productService);
            });
            builder.Services.AddSingleton(new AdminService(tableServiceClient));
            builder.Services.AddScoped<BlobImageService>();
            builder.Services.AddSingleton<ILogReader, FileLogReader>();
            builder.Services.AddSingleton<OrderLogService>();

            // Register OrderService with both dependencies
            builder.Services.AddSingleton(sp =>
            {
                var orderQueueService = sp.GetRequiredService<OrderPlacedQueueService>();
                var stockReminderQueueService = sp.GetRequiredService<StockReminderQueueService>();
                var orderLogService = sp.GetRequiredService<OrderLogService>();
                return new OrderService(tableServiceClient, orderQueueService, stockReminderQueueService,orderLogService);
            });

            // ----- AZURE FILE SHARE LOGGING -------
            // Toggle between legacy file logging and Azure File Share logging
            var useFileShareLogs =
                bool.TryParse(Environment.GetEnvironmentVariable("USE_AZURE_FILE_SHARE_LOGS"), out var flag)
                && flag;

            // Always register the ShareServiceClient (needed for the Azure branch)
            builder.Services.AddSingleton(sp =>
                new ShareServiceClient(connectionString)
            );

            if (useFileShareLogs)
            {
                // Azure File Share path resolver
                var fileShareName = Environment.GetEnvironmentVariable("AzureFileShareName");
                if (string.IsNullOrWhiteSpace(fileShareName))
                    throw new Exception("AzureFileShareName environment variable not found.");

                builder.Services.AddSingleton<ILogPathResolver>(sp =>
                    new AzureFileSharePathResolver(
                        sp.GetRequiredService<ShareServiceClient>(),
                        fileShareName
                    )
                );

                // Azure File Share log writer
                builder.Services.AddSingleton<ILogWriter>(sp =>
                    new AzureFileShareLogWriter(
                        sp.GetRequiredService<ShareServiceClient>(),
                        fileShareName,
                        sp.GetRequiredService<ILogPathResolver>()
                    )
                );
            }
            else
            {
                // Legacy UNC mount path only in legacy mode
                Environment.SetEnvironmentVariable(
                    "LogBasePath",
                    @"\\st10118454.file.core.windows.net\abc-retail-logs"
                );

                // Legacy file path resolver & writer
                builder.Services.AddSingleton<ILogPathResolver>(sp =>
                    new FileLogPathResolver(
                        Environment.GetEnvironmentVariable("LogBasePath")
                    )
                );
                builder.Services.AddSingleton<ILogWriter, FileLogWriter>();
            }

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
