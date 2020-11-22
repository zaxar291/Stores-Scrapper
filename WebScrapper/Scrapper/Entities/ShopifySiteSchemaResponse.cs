using System.Collections.Generic;

namespace WebApplication.Scrapper.Entities
{
    public class ShopifySiteSchemaResponse
    {
        public ShopifySiteSchemaResponseProduct product { get; set; }
        public ShopifySiteSchemaResponsePage page { get; set; }
    }

    public class ShopifySiteSchemaResponseProduct
    {
        public string id { get; set; }
        public string git { get; set; }
        public string vendor { get; set; }
        public string type { get; set; }
        public string sku { get; set; }
        public string firstCategory { get; set; }
        public string allCategories { get; set; } 
        public List<ShopifySiteSchemaResponseProductVariants> variants { get; set; }
    }

    public class ShopifySiteSchemaResponseProductVariants
    {
        public string id { get; set; }
        public string price { get; set; }
        public string name { get; set; }
        public string public_title { get; set; }
        public string sku { get; set; }
    }

    public class ShopifySiteSchemaResponsePage
    {
        public string pageType { get; set; }
        public string resourceType { get; set; }
        public string resourceId { get; set; }
    }
}