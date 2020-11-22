using System.Collections.Generic;
using WebApplication.Scrapper.Entities;
using WebApplication.Scrapper.Abstraction;
using WebApplication.Scrapper.Implementation;

using WebScrapper.Scrapper.Entities.enums;

namespace WebApplication.Scrapper.Abstraction
{
    interface IScrapperInstance 
    {
        ScrapperBaseStates scrapperStatus { get; set; }
        List<string> processLists { get; set; }
        LogsWriter _logger { get; set; }
        void Boot();
    }
}