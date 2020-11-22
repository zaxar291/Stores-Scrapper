using System.Collections.Generic;

namespace WebScrapper.Scrapper.Entities.StrategiesEntities.Driver {
    public class BaseWebDriverStrategy {
        public string RequestUrl { get; set; }
        public bool UseProxy = false;
        public bool LaunchIncognito = false;
        public bool IgnoreCertificateErrors = true;
        public bool DisableInfoBar = true;
        public bool LaunchHeadless = false;
        public List<BaseWebDriverTaskStrategy> TasksList { get; set; }
    }
}