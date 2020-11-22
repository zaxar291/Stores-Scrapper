using Newtonsoft.Json;

namespace WebApplication.Scrapper.Services.Akeneo.Entities.Dto.IndexedCategoriesDto
{
    public class AkeneoIndexedCategoriesDtoLinksCollection
    {
        [JsonProperty("self")]
        public AkeneoIndexedCategoriesDtoLinksCollectionEntity SelfLink { get; set; }
        [JsonProperty("first")]
        public AkeneoIndexedCategoriesDtoLinksCollectionEntity FirstLink { get; set; }
        [JsonProperty("next")]
        public AkeneoIndexedCategoriesDtoLinksCollectionEntity NextLink { get; set; }
    }
}