using Newtonsoft.Json;
using System.Collections.Generic;

namespace WebApplication.Scrapper.Services.Akeneo.Entities.Dto.IndexedCategoriesDto
{
    public class AkeneoIndexedCategoriesDtoEmbedCollectionItem
    {
        [JsonProperty("code")]
        public string CategoryCode { get; set; }

        [JsonProperty("parent")]
        public string CategoryParent { get; set; }

        [JsonProperty("labels")]
        public Dictionary<string, string> CategoriesLocales { get; set; } 
    }
}