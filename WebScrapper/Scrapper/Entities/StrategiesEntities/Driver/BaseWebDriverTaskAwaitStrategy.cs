using WebScrapper.Scrapper.Entities.enums;

namespace WebScrapper.Scrapper.Entities.StrategiesEntities.Driver
{
    public class BaseWebDriverTaskAwaitStrategy
    {
        public int AwaitMaxAttempts { get; set; }
        public int AwaitDelayTime { get; set; }
        public BaseWebDriverAwaitTaskAction AwaitCheckingStrategy { get; set; } 
    }
}