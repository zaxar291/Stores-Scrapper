using Newtonsoft.Json;

namespace WebScrapper.Scrapper.Services.Akeneo.Entities
{
    public class AkeneoCategoriesExcludedCollectionEntity
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}