using System;

namespace WebApplication.Scrapper.Entities
{
    public class WebScrapperBaseScrappingErrorMessages
    {
        #region ScrapperErrorMessages
        public string WebScrapperNameSelectorError = "#Iternal scrapping name error";
        public string WebScrapperImageSelectorError = "#Iternal scrapping image error";
        public string WebScrapperVendorSelectorError = "#Iternal scrapping vendor error";
        public string WebScrapperCategorySelectorError = "#Iternal scrapping category error";
        public string WebScrapperDefaultProductId = "error";
        public string WebScrapperDescriptionSelectorError = "That's default description text, it means, that some error occured, furing parsing process, you can find more information in the scrapper logs";
        public string WebScrapperImageCollectionSelectorError = "#Iternal scrapping image selector error";
        public string WebScrapperPriceSelectorError = "#Iternal scrapping price selector error";
        public string WebScrapperStockSelectorError = "#Iternal scrapping stock selector error, cannot find element by current selector on the page";
        #endregion

        #region NodesErrorMessages

        public string WebScrapperEmptyNodeError = "#Empty or invalid node name";
        public string WebScrapperEmptyButtonText = "#Empty text in comparer presented";

        #endregion
    }
}