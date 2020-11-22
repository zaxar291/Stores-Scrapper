using System;
using System.Collections.Generic;

using WebApplication.Scrapper.Services.Akeneo.Entities;

using WebScrapper.Scrapper.Abstraction;
using WebScrapper.Scrapper.Entities.StrategiesEntities.Driver;

namespace WebApplication.Scrapper.Abstraction
{
    public abstract class AbstractBaseScrapper <TBrowserNode, TStrategyEntity>
    {
        protected abstract TBrowserNode BrowserNode { get; set; }
        protected abstract BaseNodeHandler<TBrowserNode> NodeHandler { get; set; }
        protected abstract BaseLogger _l { get; set; }
        public abstract AkeneoProduct ScrappingInstance(TStrategyEntity ScrappingStrategy);
        protected abstract void ScrapCollectionByNode(TStrategyEntity ScrappingStrategy);
        protected abstract void ProcessWebDriverEntities(IWebDriverResolver WebDriver, BaseWebDriverStrategy strategy);
    }
}