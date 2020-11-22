using WebScrapper.Scrapper.Services.Shopify.Abstraction;

namespace WebScrapper.Scrapper.Services.Shopify.Entities
{
    public class ShopifyProduct : IBaseShopifyProduct
    {
        public string ProductName { get; set; }
        public string ProductId { get; set; }
    }
}