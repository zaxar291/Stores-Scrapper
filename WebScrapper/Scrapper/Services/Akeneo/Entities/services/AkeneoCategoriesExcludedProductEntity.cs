using Newtonsoft.Json;

namespace WebScrapper.Scrapper.Services.Akeneo.Entities
{
    public class AkeneoCategoriesExcludedProductEntity
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}