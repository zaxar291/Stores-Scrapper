using WebScrapper.Scrapper.Services.Shopify.Abstraction;

namespace WebScrapper.Scrapper.Services.Shopify.Entities
{
    public class BaseShopifyProductEntity
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductVendor { get; set; }
    }
}
