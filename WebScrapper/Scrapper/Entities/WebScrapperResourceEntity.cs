using System;
using WebScrapper.Scrapper.Entities.enums;

namespace WebScrapper.Scrapper.Entities
{
    public class WebScrapperResourceEntity
    {
        public ScrapperBaseStates ScrapperStatus { get; set; }
        public DateTime ValidUntill { get; set; }
    }
}