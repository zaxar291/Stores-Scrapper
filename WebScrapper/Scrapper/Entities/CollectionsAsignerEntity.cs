using Newtonsoft.Json;

namespace WebScrapper.Scrapper.Entities
{
    public class CollectionsAsignerEntity
    {
        [JsonProperty("compared")]
        public string CollectionToMatch { get; set; }

        [JsonProperty("shopify")]
        public string CollectionToAssign { get; set; }
    }
}