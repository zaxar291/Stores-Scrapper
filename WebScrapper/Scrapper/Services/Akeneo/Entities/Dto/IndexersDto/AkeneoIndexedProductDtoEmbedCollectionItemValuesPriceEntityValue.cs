using Newtonsoft.Json;

namespace WebApplication.Scrapper.Services.Akeneo.Entities.Dto.IndexersDto
{
    public class AkeneoIndexedProductDtoEmbedCollectionItemValuesPriceEntityValue
    {
        [JsonProperty("amount")]
        public double EntityAmount { get; set; }
        [JsonProperty("currency")]
        public string EntityCurrency { get; set; }
    }
}