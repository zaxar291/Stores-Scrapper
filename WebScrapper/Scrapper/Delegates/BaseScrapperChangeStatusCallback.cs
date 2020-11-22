using WebScrapper.Scrapper.Entities;

namespace WebScrapper.Scrapper.Delegates 
{
    public delegate void BaseScrapperChangeStatusCallback(object sender, BaseScrapperChangeStatusCallbackResult eventArgs);

    public class BaseScrapperChangeStatusCallbackResult 
    {
        public BaseScrapperChangeStatusCallbackResult(WebScrapperBaseStatuses siteStatus, string baseSiteUrl) 
        {
            SiteStatus = siteStatus;
            BaseSiteUrl = baseSiteUrl;
        }
        public WebScrapperBaseStatuses SiteStatus { get; private set; } 
        public string BaseSiteUrl { get; private set; }
    }
}