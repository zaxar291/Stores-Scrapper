using System.Collections.Generic;

using WebScrapper.Scrapper.Entities;
using WebScrapper.Scrapper.Entities.enums;
using WebScrapper.Scrapper.Delegates;

namespace WebScrapper.Scrapper.Abstraction
{
    public interface IBaseProxyService : IBaseService
    {
        BaseServicesStatuses ServiceStatus { get; set; }
        List<WebScrapperBaseProxyEntity> ProxyesList { get; set; }
        List<string> RequestProxyes { get; set; }
        string ProxyFile { get; set; }
        string ProxyFileLineDelimiter { get; set; }
        string ProxyFileEntitiesSeparator { get; set; }
        int ProxyFileHostEntityIndex { get; set; }
        int ProxyFilePortEntityIndex { get; set; }
        int ProxyFileLoginIndex { get; set; }
        int ProxyFilePasswordIndex { get; set; }
        int ProxyesLimit { get; set; }
        void LaunchProxyesChecking();
        WebScrapperBaseProxyEntity GetRandomProxy();
        List<WebScrapperBaseProxyEntity> GetRandomProxy(int len);
        int GetCurrentProxyesCount();
        event ProxyCallback OnProxyCallback;
    }
}