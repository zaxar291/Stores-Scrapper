using System.Collections.Generic;

namespace WebApplication.Scrapper.Entities
{
    public class WebScrapperBaseTagsEntityDto
    {
        public List<string> collections { get; set; }
        public string vendor { get; set; }
        public string type { get; set; }
        public bool available { get; set; }
        public string inventory_quantity { get; set; }
        public string sku { get; set; }
        public string firstCategory { get; set; }
        public string productTags { get; set; }
    }

    public class WenScrapperBaseTagsCollectionsEntityDto
    {
        public string tag { get; set; }
    }
}