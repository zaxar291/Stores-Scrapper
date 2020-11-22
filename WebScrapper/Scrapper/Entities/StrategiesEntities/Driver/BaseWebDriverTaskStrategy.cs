using WebScrapper.Scrapper.Entities.enums;

namespace WebScrapper.Scrapper.Entities.StrategiesEntities.Driver {
    public class BaseWebDriverTaskStrategy {
        public BaseWebDriverTasksTypes TaskType { get; set; }
        public string RequestElement { get; set; }
        public string NewValue { get; set; }
        public bool LoadDriverDependencies = false;
        public string RequestUrl { get; set; }
        public string ScriptSource { get; set; }
        public By ScriptSourceType { get; set; }
        public string AssignToField { get; set; }
        public string ObtainFromField { get; set; }
        public int DelayTime { get; set; }
        public BaseWebDriverTaskAwaitStrategy AwaitParams { get; set; }
        public readonly string[] DriverDependencies = {"DriverBaseScripts.js"};
    }
}