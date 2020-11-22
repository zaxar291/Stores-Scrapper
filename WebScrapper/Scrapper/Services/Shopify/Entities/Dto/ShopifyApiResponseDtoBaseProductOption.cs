using Newtonsoft.Json;
using System.Collections.Generic;

namespace WebScrapper.Scrapper.Services.Shopify.Entities.Dto
{
    class ShopifyApiResponseDtoBaseProductOption
    {
        [JsonProperty("id")]
        public string OptionId { get; set; }

        [JsonProperty("product_id")]
        public string OptionProductId { get; set; }

        [JsonProperty("name")]
        public string OptionName { get; set; }

        [JsonProperty("position")]
        public string OptionPosition { get; set; }

        [JsonProperty("values")]
        public List<string> OptionValues { get; set; }
    }
}
