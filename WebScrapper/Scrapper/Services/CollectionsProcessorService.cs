using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using WebScrapper.Scrapper.Abstraction;
using WebScrapper.Scrapper.Entities.StrategiesEntities;
using WebScrapper.Scrapper.Entities.enums;
using WebScrapper.Scrapper.Implementation.Driver;
using WebScrapper.Scrapper.Entities.StrategiesEntities.Driver;

using WebApplication.Scrapper.Abstraction;
using WebApplication.Scrapper.Services;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace WebScrapper.Scrapper.Services {
    public class CollectionsProcessorService : ICollectionsProcessorService
    {
        private BaseLogger _l { get; set; }
        private AbstractWriter _f { get; set; } 
        public CollectionsProcessorService(BaseLogger _logger) 
        {
            _l = _logger;
            _f = new FilesWriter();
        }
        public WebScrapperBaseCollectionsProcessorEntity FindCollections(WebScrapperBaseCollectionsProcessorEntity strategy) 
        {
            var _s = new BaseWebDriverStrategy {};
            var o = new ChromeOptions();
            o.AddArgument("ignore-certificate-errors");
            o.AddArgument("disable-infobars");
            // o.AddArgument("--headless");
            IWebDriverResolver _driver = new ChromeDriverResolver(_s, new Entities.WebScrapperBaseProxyEntity(), strategy.DriverSettings.RequestUrl, _l);
            if (!_driver.Initialize())
            {
                _l.error($"Fatal: instance {strategy.DriverSettings.RequestUrl} cannot be initialized!");
                _driver.Dispose();
                return strategy;
            }
            if (!ReferenceEquals(strategy.DriverSettings.TasksList, null) 
                    && strategy.DriverSettings.TasksList.Count > 0)
            {
                foreach (var task in strategy.DriverSettings.TasksList)
                {
                    if (task.TaskType.Equals(BaseWebDriverTasksTypes.TaskExecuteScript))
                    {
                        string source = String.Empty;
                        switch (task.ScriptSourceType)
                        {
                            case WebScrapper.Scrapper.Entities.enums.By.FileSource :
                                source = _f.Read($"{Directory.GetCurrentDirectory()}/Scrapper/Resources/{task.ScriptSource}");
                                if (ReferenceEquals(source, String.Empty)) 
                                {
                                    _l.error($"Fatal: cannot load script from file {Directory.GetCurrentDirectory()}/Scrapper/Resources/{task.ScriptSource}");
                                    _driver.Dispose();
                                    return strategy;
                                }
                                break;
                            case WebScrapper.Scrapper.Entities.enums.By.StringSource :
                                source = task.ScriptSource;
                                break;
                        }
                        string context = _driver.GetDataFromPage(task.RequestElement, source);
                        if (ReferenceEquals(context, String.Empty))
                        {
                            _l.error($"Chrome driver: fatal, execution script failed, visit application error logs to get more information about problem, script source: {source}");
                        }
                        try
                        {
                            var l = context.Split("||||").ToList();
                            strategy.Collections = l;
                        }
                        catch (Exception e)
                        {
                            _l.error($"Error in processing script result: {e.Message} -> {e.StackTrace}");
                            _driver.Dispose();
                            return strategy;
                        }
                    }
                }
            }
            _driver.Dispose();
            return strategy;
        }
    }
}