using System.Collections.Generic;
using Newtonsoft.Json;

namespace WebApplication.Scrapper.Services.Akeneo.Entities.Dto.IndexedCategoriesDto
{
    public class AkeneoIndexedCategoriesDto
    {
        [JsonProperty("_links")]
        public AkeneoIndexedCategoriesDtoLinksCollection LinksCollection { get; set; }

        [JsonProperty("current_page")]
        public int CurrentPage { get; set; }

        [JsonProperty("_embedded")]
        public AkeneoIndexedCategoriesDtoEmbedCollection Embed { get; set; }
    }
}