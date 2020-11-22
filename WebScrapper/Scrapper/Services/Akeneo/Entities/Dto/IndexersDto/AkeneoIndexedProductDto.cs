using System.Collections.Generic;
using Newtonsoft.Json;

namespace WebApplication.Scrapper.Services.Akeneo.Entities.Dto.IndexersDto
{
    public class AkeneoIndexedProductDto
    {
        [JsonProperty("_links")]
        public AkeneoIndexedProductDtoLinksCollection LinksCollection { get; set; }
        [JsonProperty("current_page")]
        public int CurrentPage { get; set; }
        [JsonProperty("_embedded")]
        public AkeneoIndexedProductDtoEmbedCollection Embed { get; set; }
    }
}