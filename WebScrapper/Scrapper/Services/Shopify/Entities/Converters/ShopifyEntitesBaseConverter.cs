using WebApplication.Scrapper.Services.Akeneo.Abstraction;

using WebScrapper.Scrapper.Services.Shopify.Entities.Dto;
using WebScrapper.Scrapper.Services.Shopify.Entities;

namespace WebScrapper.Scrapper.Services.Shopify.Entities.Converters
{
    class ShopifyEntitesBaseConverter : BaseConverter<ShopifyApiResponseDtoBaseProduct, BaseShopifyProductEntity>
    {
        public override BaseShopifyProductEntity ConvertToApplicationEntity(ShopifyApiResponseDtoBaseProduct dto)
        {
            var Product = new BaseShopifyProductEntity
            {
                ProductId = dto.ProductIdentifier,
                ProductName = dto.ProductTitle,
                ProductVendor = dto.ProductVendor
            };

            return Product;
        }

        public override ShopifyApiResponseDtoBaseProduct ConvertToDtoEntity(BaseShopifyProductEntity data)
        {
            var product = new ShopifyApiResponseDtoBaseProduct();
            return product;
        }
    }
}
