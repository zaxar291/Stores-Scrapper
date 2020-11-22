using WebScrapper.Scrapper.Entities.enums;

namespace WebScrapper.Scrapper.Entities.StrategiesEntities {
    class BaseHtmlItemStrategyProcessingStrategy {
        public BaseWebScrapperRules RuleStrategy { get; set; }
        public string RuleSeparator { get; set; }
    }
}