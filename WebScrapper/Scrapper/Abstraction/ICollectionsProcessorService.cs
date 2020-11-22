using WebScrapper.Scrapper.Entities.StrategiesEntities;

namespace WebScrapper.Scrapper.Abstraction 
{
    public interface ICollectionsProcessorService
    {
        WebScrapperBaseCollectionsProcessorEntity FindCollections(WebScrapperBaseCollectionsProcessorEntity entity);
    }
}