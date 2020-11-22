using System.Collections.Generic;

namespace WebApplication.Scrapper.Entities.StrategiesEntities {
    public class BaseItemStrategyAttribute {
        public string AttributeName { get; set; }
        public BaseHtmlItemStrategyValidationRule AttributeValidationRule { get; set; }
        public string AttributeAssingToRule { get; set; }
    }
}