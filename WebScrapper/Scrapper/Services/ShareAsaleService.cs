using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

using WebApplication.Scrapper.Abstraction;

using WebScrapper.Scrapper.Abstraction;
using WebScrapper.Scrapper.Entities.enums;
using WebScrapper.Scrapper.Entities;
using WebScrapper.Scrapper.Implementation.Driver;
using WebScrapper.Scrapper.Entities.StrategiesEntities.Driver;

namespace WebScrapper.Scrapper.Services
{
    public class ShareAsaleService : IBaseService, IDisposable
    {
        private object locker;
        private BaseWebDriverStrategy _linksGenerationTask;
        private BaseWebDriverStrategy _driverTasks;
        private WebScrapperBaseProxyEntity Proxy;
        private ChromeDriverResolver _c;
        private BaseLogger _l;
        private ShareAsaleSettings settings;
        private List<ShareAsaleLinkEntity> _tasks;
        private BaseServicesStatuses ServiceStatus { get; set; }
        private readonly Timer _t;
        private bool ShareLockLogic = false;
        public string ServiceName { get; set; }
        public ShareAsaleService(ShareAsaleSettings _s, BaseLogger logger)
        {
            settings = _s;
            _l = logger;

            ServiceStatus = BaseServicesStatuses.ServiceNotLaunched;
            _tasks = new List<ShareAsaleLinkEntity>();
            locker = new object();
            InitTasks();

            var callback = new TimerCallback(Timer_Callback);
            _t = new Timer(callback, null, 0, 2000);
        }

        public void LaunchService()
        {
            ServiceStatus = BaseServicesStatuses.ServiceLaunching;
            try
            {
                _c = new ChromeDriverResolver(_driverTasks, Proxy, settings.BaseUrl, _l);
                if (!_c.Initialize())
                {
                    ServiceStatus = BaseServicesStatuses.ServiceError;
                    return;
                }
                Thread.Sleep(15000);
                _c.UpdateFieldData("#username", settings.Login);
                _c.UpdateFieldData("#password", settings.Password);
                _c.ExecuteScript("document.getElementById('form1').submit()");
                Thread.Sleep(5000);
                ServiceStatus = BaseServicesStatuses.ServiceLaunched;
            }
            catch (Exception e)
            {
                ServiceStatus = BaseServicesStatuses.ServiceError;
                _l.error($"[ShareAsaleService] An exception occured, during initializing chrome driver: {e.Message} -> {e.StackTrace}");
                _c?.Dispose();
            }
        }

        public void AddLinkToProcessing(string request)
        {
            _tasks.Add(new ShareAsaleLinkEntity {
                RequestUrl = request
            });
        }

        public string GetLinkSolution(string request)
        {
            if (!ServiceStatus.Equals(BaseServicesStatuses.ServiceLaunched))
            {
                return null;
            }
            lock (locker)
            {
                var current = _tasks.FirstOrDefault(t => t.RequestUrl.Equals(request));
                if (!ReferenceEquals(current, null))
                {
                    return current.GeneratedUrl;
                }
            }
            return null;
        }

        private void Timer_Callback(object obj)
        {
            ProcessLinks();
        }

        private void ProcessLinks()
        {
            if (ShareLockLogic)
            {
                return;
            }
            // if (!ReferenceEquals(ServiceStatus, BaseServicesStatuses.ServiceLaunched))
            // {
            //     return;
            // }
            if (ReferenceEquals(_tasks, null) || _tasks.Count.Equals(0))
            {
                return;
            }
            if (ReferenceEquals(_tasks.Where(t => ReferenceEquals(t.GeneratedUrl, null) || t.GeneratedUrl.Equals(String.Empty)).ToList(), 0))
            {
                return;
            }
            ShareLockLogic = true;
            var tasks = _tasks.Where(t => ReferenceEquals(t.GeneratedUrl, null) || t.GeneratedUrl.Equals(String.Empty)).ToList();
            foreach (var task in tasks)
            {
                if (!ReferenceEquals(_c, null))
                {
                    _c.NavigateToPage(settings.RequestPageUrl);
                    _c.UpdateFieldData("#destinationURL", task.RequestUrl);
                    _c.ExecuteScript("document.getElementById('buildLinkFrm').children[7].children[0].click()");
                    bool solved = false;
                    int safe = 10;
                    int current = 0;
                    while (!solved && current <= safe)
                    {
                        Thread.Sleep(500);
                        var currentContext = _c.GetDataFromPage(null, "return getUrl(); function getUrl(){return document.getElementById(\"buildLinkFrm\").children[7].children[0].getAttribute(\"value\");}").Trim();
                        if (currentContext.Equals("Create Custom Link"))
                        {
                            current++;
                        }   
                        else 
                        {
                            break;
                        }
                    }
                    task.GeneratedUrl = _c.GetDataFromPage(null, "return getUrl(); function getUrl(){return document.getElementById(\"buildLinkFrm\").children[7].children[0].getAttribute(\"value\");}");
                    lock (locker)
                    {
                        var _selected = _tasks.FirstOrDefault(t => t.RequestUrl.Equals(task.RequestUrl));
                        if (!ReferenceEquals(_selected, null))
                        {
                            _selected.GeneratedUrl = _c.GetDataFromPage(null, "return getUrl(); function getUrl(){return document.getElementById(\"buildLinkFrm\").children[7].children[0].getAttribute(\"value\");}");
                        }
                    }
                }
            }
            ShareLockLogic = false;
            // ProcessLinks();
        }

        protected void InitTasks()
        {
            _driverTasks = new BaseWebDriverStrategy {
                RequestUrl = settings.BaseUrl,
                TasksList = new List<BaseWebDriverTaskStrategy>(),
                UseProxy = false,
                LaunchIncognito = false,
                LaunchHeadless = false,
                DisableInfoBar = true,
                IgnoreCertificateErrors = true
            };

            _driverTasks.TasksList.Add(new BaseWebDriverTaskStrategy {
                TaskType = BaseWebDriverTasksTypes.TaskUpdateFieldData,
                RequestElement = "#username",
                NewValue = "dev-artjoker"
            });

            _driverTasks.TasksList.Add(new BaseWebDriverTaskStrategy {
                TaskType = BaseWebDriverTasksTypes.TaskUpdateFieldData,
                RequestElement = "#password",
                NewValue = "ef(*K:P=6%`xFb*=",
            });

            _driverTasks.TasksList.Add(new BaseWebDriverTaskStrategy {
                TaskType = BaseWebDriverTasksTypes.TaskExecuteScript,
                ScriptSource = "document.getElementById('form1').submit()"
            });
        }

        public BaseServicesStatuses GetServiceStatus()
        {
            return ServiceStatus;
        }

        public void Dispose() 
        {
            _c?.Dispose();
        }
    }
}