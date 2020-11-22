using Newtonsoft.Json;

namespace WebScrapper.Scrapper.Entities.BaseScrapperEntities 
{
    public class BaseScrapperShopifyProductTemplate 
    {
        public BaseScrapperShopifyProductTemplateProduct product { get; set; } 
    }

    public class BaseScrapperShopifyProductTemplateProduct 
    {
        [JsonProperty("id")]
        public string ProductId { get; set; }
        [JsonProperty("title")]
        public string ProductName { get; set; }
        [JsonProperty("tags")]
        public string[] ProductTags { get; set; }
        public string ProductLimitedTags { get; set; }
    }
}