using System.Collections.Generic;

using WebScrapper.Scrapper.Entities.enums;
using WebScrapper.Scrapper.Entities.StrategiesEntities.Driver;

namespace WebScrapper.Scrapper.Entities.StrategiesEntities
{
    public class WebScrapperBaseCollectionsProcessorEntity
    {
        public bool IsCategoriesPreparingRequired { get; set; }
        public WebScrapperBaseCollectionsProcessorEntityProcessingType ProcessingType { get; set; }
        public BaseWebDriverStrategy DriverSettings { get; set; }
        public List<string> Collections { get; set; } 
    }
}