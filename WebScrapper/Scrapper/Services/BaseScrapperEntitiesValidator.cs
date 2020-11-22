using WebApplication.Scrapper.Entities.StrategiesEntities;
using WebApplication.Scrapper.Abstraction;

namespace WebApplication.Scrapper.Services {

    public class BaseScrapperEntitiesValidator : IBaseScrapperValidationService<BaseHtmlItemStrategyValidationRule> {

        public const string IsNullValidationRule = "Is.Null";
        public const string IsEqualsValidationRule = "Is.Equals";

        public bool ValidateByRule(object input, BaseHtmlItemStrategyValidationRule rule) {
            if (ReferenceEquals(rule, null)) {
                return false;
            }
            if (ReferenceEquals(input, null) && !rule.ValidationRule.Equals(IsNullValidationRule)) {
                return false;
            }
            if (rule.ValidationRule.Equals(string.Empty)) {
                return false;
            }
            switch (rule.ValidationRule) {
                case IsNullValidationRule :
                    return ValidateAsNull(input, rule);
                case IsEqualsValidationRule :
                    return ValidateAsEquals(input.ToString(), rule);
            }
            return false;
        }

        public bool ValidateAsNull(object input, BaseHtmlItemStrategyValidationRule rule) {
            return ReferenceEquals(input, null);
        } 

        public bool ValidateAsEquals(string input, BaseHtmlItemStrategyValidationRule rule) {
            return input.Trim().Equals(rule.ComparedString);
        }
    }
     
}