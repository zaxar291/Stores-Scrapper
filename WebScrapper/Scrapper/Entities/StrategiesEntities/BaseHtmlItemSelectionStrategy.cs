using System.Collections.Generic;

using WebApplication.Scrapper.Entities.enums;

namespace WebApplication.Scrapper.Entities.StrategiesEntities {
    public class StrategyHtmlEntity {
        public ScrapperHtmlStrategyTypes SelectionStrategy { get; set; }
        public StrategyHtmlSelectionType SelectionType { get; set; }
        public string BaseItemSelector { get; set; }
        public List<BaseItemStrategyAttribute> AttributesList { get; set; }
        public BaseHtmlItemStrategyValidationRule ValidationRule { get; set; }
        public string AssignEntityTo { get; set; }
        public string SelectByIndexFromRange { get; set; }
        public bool AutoDetect = false;
        public string DetectType { get; set; }
    }
}