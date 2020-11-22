namespace WebApplication.Scrapper.Entities.StrategiesEntities {
    public class BaseHtmlItemStrategyValidationRule {
        public string ValidationRule { get; set; }
        public string ComparedString { get; set; }
        public object ResultIfPassed { get; set; }
        public object ResultIfFailed { get; set; }  
    }
}