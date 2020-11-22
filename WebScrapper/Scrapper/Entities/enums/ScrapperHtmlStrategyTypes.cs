namespace WebApplication.Scrapper.Entities.enums {
    public enum ScrapperHtmlStrategyTypes {
        StrategyInnerHtmlSelection = 1,
        StrategyInnerTextSelection = 2,
        StrategyAttributesSelection = 3,
        StrategySchemaOrgSelection = 4,
        StrategyShopifyMetaScriptSelection = 5,
        StrategyShopifyJsonProductTemplate = 6,
        StrategyShopifyProductJsonTemplate = 9,

        #region CustomSelections
        StrategyPuffingBirdTags = 7,
        StrategyGrassCityJs = 8,
        StrategyGraphSelection = 10
        #endregion
    }
}