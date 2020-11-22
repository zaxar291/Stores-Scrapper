using Newtonsoft.Json;

namespace WebApplication.Scrapper.Services.Akeneo.Entities.Dto.IndexedCategoriesDto
{
    public class AkeneoIndexedCategoriesDtoLinksCollectionEntity
    {
        [JsonProperty("href")]
        public string Href { get; set; }
    }
}