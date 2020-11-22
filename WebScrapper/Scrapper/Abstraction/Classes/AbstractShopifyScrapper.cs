using System;
using System.Collections.Generic;

namespace WebApplication.Scrapper.Abstraction
{
    public abstract class AbstractShopifyScrapper<D>
    {
        private string requestUrl { get; set; }

        private List<String> linksPool;

        private BaseLogger _logger { get; set; }
        
        public abstract void Scrap(object sender, D eventArgs);

        public abstract void ScrapLinks();

        public abstract void ProcessXmlShopifyTree();

        public abstract void ProcessLinksCollection();
    }
}