using Newtonsoft.Json;
using System.Collections.Generic;

namespace WebScrapper.Scrapper.Services.Shopify.Entities.Dto
{
    class ShopifyBaseDtoObject
    {
        [JsonProperty("products")]
        public List<ShopifyApiResponseDtoBaseProduct> ProductsCollection { get; set; }

        [JsonProperty("product")]
        public ShopifyApiResponseDtoBaseProduct Product { get; set; }
    }
}
