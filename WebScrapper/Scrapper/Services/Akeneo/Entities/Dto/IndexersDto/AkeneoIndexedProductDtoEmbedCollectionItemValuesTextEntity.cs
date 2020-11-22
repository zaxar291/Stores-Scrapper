using Newtonsoft.Json;

namespace WebApplication.Scrapper.Services.Akeneo.Entities.Dto.IndexersDto
{
    public class AkeneoIndexedProductDtoEmbedCollectionItemValuesTextEntity
    {
        [JsonProperty("locale")]
        public string EntityLocale { get; set; }
        [JsonProperty("scope")]
        public string EntityScope { get; set; }
        [JsonProperty("data")]
        public string EntityData { get; set; }
    }
}