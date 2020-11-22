using WebApplication.Scrapper.Services.Akeneo.Abstraction;
using WebApplication.Scrapper.Services.Akeneo.Entities;

using WebScrapper.Scrapper.Services.Shopify.Entities;

namespace WebScrapper.Scrapper.Services.Shopify.Entities.Converters
{
    internal sealed class ShopifyAkeneoEntityConverter : BaseConverter<AkeneoProduct, ShopifyProduct>
    {
        public override ShopifyProduct ConvertToApplicationEntity(AkeneoProduct dto)
        {
            var product = new ShopifyProduct();
            return product;
        }
    }
}