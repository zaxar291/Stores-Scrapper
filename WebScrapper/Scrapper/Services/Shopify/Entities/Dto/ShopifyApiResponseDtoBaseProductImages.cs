using Newtonsoft.Json;

namespace WebScrapper.Scrapper.Services.Shopify.Entities.Dto
{
    class ShopifyApiResponseDtoBaseProductImages
    {
        [JsonProperty("id")]
        public string ImageId { get; set; }

        [JsonProperty("product_id")]
        public string ImageProductId { get; set; }

        [JsonProperty("position")]
        public string ImagePosition { get; set; }

        [JsonProperty("created_at")]
        public string ImageCreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public string ImageUpdatedAt { get; set; }

        [JsonProperty("alt")]
        public string ImageAlt { get; set; }

        [JsonProperty("width")]
        public int ImageWidth { get; set; }

        [JsonProperty("height")]
        public int ImageHeight { get; set; }

        [JsonProperty("variant_ids")]
        public string[] ImageVariantsIds { get; set; }

        [JsonProperty("admin_graphql_api_id")]
        public string ImageAminGraphqlApiId { get; set; }
    }
}
