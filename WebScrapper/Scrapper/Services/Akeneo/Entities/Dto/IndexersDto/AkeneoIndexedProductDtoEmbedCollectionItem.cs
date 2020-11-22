using Newtonsoft.Json;

namespace WebApplication.Scrapper.Services.Akeneo.Entities.Dto.IndexersDto
{
    public class AkeneoIndexedProductDtoEmbedCollectionItem
    {
        [JsonProperty("_links")]
        public AkeneoIndexedProductDtoLinksCollection LinksCollection { get; set; }
        [JsonProperty("identifier")]
        public string Identifier { get; set; }
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }
        [JsonProperty("family")]
        public string ProductFamily { get; set; }
        [JsonProperty("categories")]
        public object ProductCategories { get; set; }
        [JsonProperty("groups")]
        public object ProductGroups { get; set; }
        [JsonProperty("parent")]
        public object ProductParent { get; set; }
        [JsonProperty("values")]
        public AkeneoIndexedProductDtoEmbedCollectionItemValues ProductValues { get; set; }
        [JsonProperty("created")]
        public string ProductCreated { get; set; }
        [JsonProperty("updated")]
        public string ProductUpdated { get; set; }
    }
}