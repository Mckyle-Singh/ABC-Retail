using ABC_Retail.Models.DTOs;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Text.Json;

namespace ABC_Functions;

public class ProductChangeFunction
{
    private readonly ILogger<ProductChangeFunction> _logger;

    public ProductChangeFunction(ILogger<ProductChangeFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(ProductChangeFunction))]
    public void Run([QueueTrigger("product-updates-queue", Connection = "AzureStorageConnection")] string jsonMessage)
    {

        try
        {
            var dto = JsonSerializer.Deserialize<ProductChangeMessageDto>(jsonMessage);
            _logger.LogInformation("Processed product update for {Name} with stock {StockQty}", dto?.Name, dto?.StockQty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process product update message.");
        }


    }
}