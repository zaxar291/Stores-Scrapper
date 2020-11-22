using WebScrapper.Scrapper.Entities.StrategiesEntities;

namespace WebScrapper.Scrapper.Abstraction {
    interface IBaseScrapperProcessingService {
        public object ProcessStringByRule(string processable, BaseHtmlItemStrategyProcessingStrategy strategy);
    }
}