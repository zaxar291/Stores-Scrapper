using WebScrapper.Scrapper.Entities.enums;

namespace WebScrapper.Scrapper.Abstraction
{
    public interface IBaseScrapperEntity
    {
        string ItemUrl { get; set; }
        string BaseSiteUrl { get; set; }
        string ExternalHash { get; set; }
        WebScrapperSiteTypes SitePlatform { get; set; }
    }
}