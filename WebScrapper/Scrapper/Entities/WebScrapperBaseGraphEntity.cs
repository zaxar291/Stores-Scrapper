using System.Collections.Generic;
using Newtonsoft.Json;

namespace WebScrapper.Scrapper.Entities
{
    public class WebScrapperBaseGraphEntity 
    {
        [JsonProperty("@context")]
        public string Context { get; set; }
        [JsonProperty("@graph")]
        public List<WebScrapperBaseGraphEntityType> Graph { get; set; }
    }

    public class WebScrapperBaseGraphEntityType
    {
        [JsonProperty("@type")]
        public string Type { get; set; }

        [JsonProperty("sku")]
        public string Sku { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("description")]
        public string Description { get; set; }
        
        [JsonProperty("releaseDate")]
        public string ReleaseDate { get; set; }
        
        [JsonProperty("category")]
        public string Category { get; set; }
        
        [JsonProperty("offers")]
        public WebScrapperBaseGraphEntityOffer Offers { get; set; }
        public string Price { get; set; }
        public bool IsProductInStock { get; set; }
    }

    public class WebScrapperBaseGraphEntityOffer 
    {
        [JsonProperty("@type")]
        public string Type { get; set; }

        [JsonProperty("price")]
        public string Price { get; set; }
        [JsonProperty("lowPrice")]
        public string LowPrice { get; set; }
        
        [JsonProperty("highPrice")]
        public string HighPrice { get; set; }
        
        [JsonProperty("offerCount")]
        public string OfferCount { get; set; }
        
        [JsonProperty("priceCurrency")]
        public string PriceCurrency { get; set; }
        
        [JsonProperty("availability")]
        public string Availability { get; set; }
    }
}