using System.Collections.Generic;

using WebApplication.Scrapper.Entities.StrategiesEntities;

using WebScrapper.Scrapper.Abstraction;
using WebScrapper.Scrapper.Entities.StrategiesEntities;
using WebScrapper.Scrapper.Entities.StrategiesEntities.Driver;
using WebScrapper.Scrapper.Entities.enums;

namespace WebScrapper.Scrapper.Entities
{
    public class WebScrapperBaseSiteEntity : IBaseScrapperEntity
    {
        #region Base parameters
        public string ItemUrl { get; set; }
        public string BaseSiteUrl { get; set; }
        public string ProductFamily { get; set; }
        public string ExternalHash { get; set; }
        public WebScrapperSiteTypes SitePlatform { get; set; }
        public string SiteProductPageIndicationSelector { get; set; }
        public WebScrapperBaseCollectionsProcessorEntity CollectionsProcessor { get; set; }
        public bool DefaultStockValue { get; set; }
        public string DefaultBrand { get; set; } 
        public bool UseShareAsaleGeneration = false;
        #endregion
        #region Selectors
        public List<StrategyHtmlEntity> ScrappingElements { get; set; }
        #endregion
        #region ExcludesRules
        public List<string> ExcludeUrlsByParts { get;set; }
        #endregion
        #region Driver settings
        public BaseWebDriverStrategy DriverStrategy { get; set; }
        #endregion
        #region Timings
        public int SiteBaseRequestsPerSecondMin { get; set; }
        public int SiteBaseRequestsPerSecondMax { get; set; }
        public int SiteBaseRequestsIntervalMin { get; set; }
        public int SiteBaseRequestsIntervalMax { get; set; }
        #endregion
        #region Technical
        public WebScrapperBaseStatuses SiteStatus = WebScrapperBaseStatuses.InstanceNotLaunched;
        #endregion
    }
}