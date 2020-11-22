using Newtonsoft.Json;

namespace WebApplication.Scrapper.Services.Akeneo.Entities.Dto.IndexersDto
{
    public class AkeneoIndexedProductDtoLinksCollectionEntity
    {
        [JsonProperty("href")]
        public string Href { get; set; }
    }
}