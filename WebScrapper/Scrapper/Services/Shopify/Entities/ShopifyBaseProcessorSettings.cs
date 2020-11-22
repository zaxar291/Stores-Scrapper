using System;
using System.Collections.Generic;
using System.Text;

namespace WebScrapper.Scrapper.Services.Shopify.Entities
{
    public class ShopifyBaseProcessorSettings
    {
        public string ShopifyStoreUrl { get; set; }
        public string ShopifyApiKey { get; set; }
        public string ShopifyApiSecret { get; set; }
        public string ShopifyApiToken { get; set; }
        public string ShopifyApiProtocol { get; set; }
        public string ShopifyBaseProuctsListUrl = "/admin/api/2020-04/products.json";
        public string ShopifyBaseProuctsUpdateUrl = "/admin/api/2020-04/products/";
        public string ShopifyBaseProuctsUpdateUrlExtension = ".json";
    }
}
