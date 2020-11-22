using System.Collections.Generic;
using Newtonsoft.Json;

namespace WebApplication.Scrapper.Services.Akeneo.Entities.Dto.IndexedCategoriesDto
{
    public class AkeneoIndexedCategoriesDtoEmbedCollection
    {
        [JsonProperty("items")]
        public List<AkeneoIndexedCategoriesDtoEmbedCollectionItem> ItemsCollection { get; set; }
    }
}