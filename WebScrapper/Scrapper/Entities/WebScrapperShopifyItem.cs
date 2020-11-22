using WebScrapper.Scrapper.Abstraction;
using WebScrapper.Scrapper.Entities.enums;

namespace WebScrapper.Scrapper.Entities
{
    public class WebScrapperShopifyItem : IBaseScrapperEntity
    {
        #region Base parameters
        
        public string ItemUrl { get; set; }
        public string BaseSiteUrl { get; set; }
        public string ProductFamily { get; set; }
        public string ExternalHash { get; set; }
        public WebScrapperSiteTypes SitePlatform { get; set; }
        #endregion

        #region Selectors
        
        public string ImageNodeSelector { get; set; }
        public string SchemaScriptNode { get; set; }
        public string DescriptionSelector { get; set; }
        public string DescriptionAttribute { get; set; }
        public string OldPriceNodeSelector { get; set; }
        public string PriceNodeSelector { get; set; }
        public string ImageNodeCollectionSelector { get; set; }
        public string ProductButtonInStockNodeSelector { get; set; }
        public string ProductButtonInStockTextIndicator { get; set; }
        public string ProductNameSelector { get; set; }
        
        #endregion
        
        #region Selection Parameters
        public bool SelectDescriptionByAttribute { get; set; }
        #endregion
    }
}