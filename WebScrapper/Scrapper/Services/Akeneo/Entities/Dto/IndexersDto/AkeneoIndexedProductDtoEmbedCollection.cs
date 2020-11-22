using System.Collections.Generic;
using Newtonsoft.Json;

namespace WebApplication.Scrapper.Services.Akeneo.Entities.Dto.IndexersDto
{
    public class AkeneoIndexedProductDtoEmbedCollection
    {
        [JsonProperty("items")]
        public List<AkeneoIndexedProductDtoEmbedCollectionItem> ItemsCollection { get; set; }
    }
}