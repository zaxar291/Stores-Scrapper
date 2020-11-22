using Newtonsoft.Json;
using System.Collections.Generic;

namespace WebScrapper.Scrapper.Services.Shopify.Entities.Dto
{
    class ShopifyApiResponseDtoBaseProduct
    {
        [JsonProperty("id")]
        public string ProductIdentifier { get; set; }

        [JsonProperty("title")]
        public string ProductTitle { get; set; }

        [JsonProperty("body_html")]
        public string ProductDescription { get; set; }

        [JsonProperty("vendor")]
        public string ProductVendor { get; set; }

        [JsonProperty("product_type")]
        public string ProductType { get; set; }

        [JsonProperty("created_at")]
        public string ProductDateCreation { get; set; }

        [JsonProperty("handle")]
        public string ProductHandle { get; set; }

        [JsonProperty("updated_at")]
        public string ProductDateupdated { get; set; }

        [JsonProperty("published_at")]
        public string ProductDatePublished { get; set; }

        [JsonProperty("template_suffix")]
        public string ProductTemplateSuffix{ get; set; }

        [JsonProperty("published_scope")]
        public string ProductPublishedScope{ get; set; }

        [JsonProperty("tags")]
        public string ProductTags { get; set; }

        [JsonProperty("admin_graphql_api_id")]
        public string ProductAminGraphApiId{ get; set; }

        [JsonProperty("variants")]
        public List<ShopifyApiResponseDtoBaseProductVariant> ProductVariants { get; set; }

        [JsonProperty("options")]
        public List<ShopifyApiResponseDtoBaseProductOption> ProductOptions { get; set; }

        [JsonProperty("images")]
        public List<ShopifyApiResponseDtoBaseProductImages> ProductImages { get; set; }

        [JsonProperty("image")]
        public ShopifyApiResponseDtoBaseProductImages Productimage { get; set; }
    }
}
