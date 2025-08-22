using ABC_Retail.Models.ViewModels;

namespace ABC_Retail.Services.Messaging
{
    public interface IProductChangeStore
    {
        void Add(ProductChangeViewModel change);
        IReadOnlyList<ProductChangeViewModel> LatestChanges { get; }


    }
}
