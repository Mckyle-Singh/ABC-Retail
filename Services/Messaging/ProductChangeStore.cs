using ABC_Retail.Models.ViewModels;

namespace ABC_Retail.Services.Messaging
{
    public class ProductChangeStore:IProductChangeStore
    {
        private readonly List<ProductChangeViewModel> _changes = new();

        public void Add(ProductChangeViewModel change)
        {
            _changes.Add(change);
        }

        public IReadOnlyList<ProductChangeViewModel> LatestChanges => _changes.AsReadOnly();


    }
}
