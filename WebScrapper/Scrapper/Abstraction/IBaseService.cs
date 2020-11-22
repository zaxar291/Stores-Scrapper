using WebScrapper.Scrapper.Entities.enums;

namespace WebScrapper.Scrapper.Abstraction
{
    public interface IBaseService
    {
        string ServiceName { get; set; }
        BaseServicesStatuses GetServiceStatus();
    }
}