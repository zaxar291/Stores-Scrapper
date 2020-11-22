using System.Collections.Generic;

using Newtonsoft.Json;
namespace WebScrapper.Scrapper.Entities.ShopifyEntities 
{
    public class ShopifyJsonProductTemplate
    {
        [JsonProperty("id")]
        public string ProductId { get; set; }

        [JsonProperty("title")]
        public string ProductTitle { get; set; }

        [JsonProperty("vendor")]
        public string ProductVendor { get; set; }  

        [JsonProperty("type")]
        public string ProductType { get; set; }  

        [JsonProperty("tags")]
        public List<string> ProductTags { get; set; }  
        public string PreparedTags { get; set; }
    } 
}