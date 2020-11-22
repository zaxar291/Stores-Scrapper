using System.Collections.Generic;
using Newtonsoft.Json;

namespace WebApplication.Scrapper.Services.Akeneo.Entities.Dto.IndexersDto
{
    public class AkeneoIndexedProductDtoEmbedCollectionItemValuesPriceEntity
    {
        [JsonProperty("locale")]
        public string EntityLocale { get; set; }
        [JsonProperty("scope")]
        public string EntityScope { get; set; }
        [JsonProperty("data")]
        public List<AkeneoIndexedProductDtoEmbedCollectionItemValuesPriceEntityValue> EntityData { get; set; }
    }
}