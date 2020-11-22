using Newtonsoft.Json;

namespace WebApplication.Scrapper.Services.Akeneo.Entities.Dto.IndexersDto
{
    public class AkeneoIndexedProductDtoLinksCollection
    {
        [JsonProperty("self")]
        public AkeneoIndexedProductDtoLinksCollectionEntity SelfLink { get; set; }
        [JsonProperty("first")]
        public AkeneoIndexedProductDtoLinksCollectionEntity FirstLink { get; set; }
        [JsonProperty("next")]
        public AkeneoIndexedProductDtoLinksCollectionEntity NextLink { get; set; }
    }
}