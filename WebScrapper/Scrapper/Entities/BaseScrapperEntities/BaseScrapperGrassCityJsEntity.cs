using Newtonsoft.Json;
using System.Collections.Generic;

namespace WebScrapper.Scrapper.Entities.BaseScrapperEntities 
{
    public class BaseScrapperGrassCityJsEntity 
    {
        [JsonProperty("ProductID")]
        public string ProductId { get; set; }

        [JsonProperty("Name")]
        public string ProductName { get; set; }

        [JsonProperty("SKU")]
        public string ProductSku { get; set; }

        [JsonProperty("URL")]
        public string ProductUrl { get; set; }

        [JsonProperty("Price")]
        public string ProductOldPrice { get; set; }

        [JsonProperty("FinalPrice")]
        public string ProductPrice { get; set; }

        [JsonProperty("Categories")]
        public List<string> ProductCategories { get; set; }

        [JsonProperty("ImageURL")]
        public string ProductImageUrl { get; set; }
    }
}