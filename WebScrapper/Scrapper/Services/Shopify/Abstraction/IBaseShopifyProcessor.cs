using WebScrapper.Scrapper.Abstraction;
using WebScrapper.Scrapper.Delegates;

namespace WebScrapper.Scrapper.Services.Shopify.Abstraction
{
    public interface IBaseShopifyProcessor : IBaseService
    {
        void ListProducts();
        bool UpdateProduct (string ProductName);
        event BaseServiceCallBack OnShopifyIndexationFinished;
    }
}
