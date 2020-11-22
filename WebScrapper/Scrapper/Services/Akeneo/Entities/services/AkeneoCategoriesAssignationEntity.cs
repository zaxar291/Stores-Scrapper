using Newtonsoft.Json;

namespace WebScrapper.Scrapper.Services.Akeneo.Entities
{
    public class AkeneoCategoriesAssignationEntity 
    {
        [JsonProperty("compared")]
        public string CategoryToSearch { get; set; }

        [JsonProperty("shopify")]
        public string CategoryToAssign { get; set; }
    }
}