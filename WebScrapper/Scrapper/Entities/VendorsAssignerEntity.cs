using Newtonsoft.Json;

namespace WebScrapper.Scrapper.Entities
{
    public class VendorsAssignerEntity
    {
        [JsonProperty("compared")]
        public string VendorToCompare { get; set; }

        [JsonProperty("shopify")]
        public string VendorToAssign { get; set; }
    }
}