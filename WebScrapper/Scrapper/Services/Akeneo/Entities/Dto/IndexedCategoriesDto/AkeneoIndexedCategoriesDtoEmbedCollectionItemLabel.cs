using Newtonsoft.Json;

namespace WebApplication.Scrapper.Services.Akeneo.Entities.Dto.IndexedCategoriesDto
{
    public class AkeneoIndexedCategoriesDtoEmbedCollectionItemLabel
    {
        [JsonProperty("localeCode")]
        public string CategoryLocaleCode { get; set; }
    }
}