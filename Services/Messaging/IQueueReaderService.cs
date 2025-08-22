using ABC_Retail.Models.Messaging;

namespace ABC_Retail.Services.Messaging
{
    public interface IQueueReaderService
    {
        Task<ProductChangeMessage?> ReadNextMessageAsync(CancellationToken cancellationToken = default);
    }
}
