using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using WebScrapper.Scrapper.Abstraction;
using WebScrapper.Scrapper.Entities.enums;
using WebScrapper.Scrapper.Entities;
using WebScrapper.Scrapper.Delegates;

using WebApplication.Scrapper.Abstraction;
using WebApplication.Scrapper.Services;

namespace WebScrapper.Scrapper.Services
{
    public class ProxyService: IBaseProxyService
    {
        public string ServiceName { get; set; }
        public BaseServicesStatuses ServiceStatus { get; set; }
        public List<WebScrapperBaseProxyEntity> ProxyesList { get; set; }
        public List<string> RequestProxyes { get; set; }
        public string ProxyFile { get; set; }
        public string ProxyFileLineDelimiter { get; set; }
        public string ProxyFileEntitiesSeparator { get; set; }
        public int ProxyFileHostEntityIndex { get; set; }
        public int ProxyFilePortEntityIndex { get; set; }
        public int ProxyFileLoginIndex { get; set; }
        public int ProxyFilePasswordIndex { get; set; }
        public int ProxyesLimit { get; set; }
        protected AbstractWriter _f { get; set; }
        protected BaseLogger _l { get; set; }
        public ProxyService(BaseLogger _logger)
        {
            _f = new FilesWriter();
            _l = _logger;
        }
        public ProxyService(BaseLogger _logger, 
                            string proxyFile)
        {
            _f = new FilesWriter();
            _l = _logger;
            ProxyFile = proxyFile;
        }
        public ProxyService(BaseLogger _logger, 
                            string proxyFile, string proxyFileLineDelimiter)
        {
            _f = new FilesWriter();
            _l = _logger;
            ProxyFile = proxyFile;
            ProxyFileLineDelimiter = proxyFileLineDelimiter;
        }
        public ProxyService(BaseLogger _logger, 
                            string proxyFile, 
                            string proxyFileLineDelimiter,
                            string proxyFileEntitiesSeparator)
        {
            _f = new FilesWriter();
            _l = _logger;
            ProxyFile = proxyFile;
            ProxyFileLineDelimiter = proxyFileLineDelimiter;
            ProxyFileEntitiesSeparator = proxyFileEntitiesSeparator;
        }
        public ProxyService(BaseLogger _logger, 
                            string proxyFile, 
                            string proxyFileLineDelimiter,
                            string proxyFileEntitiesSeparator,
                            int proxyFileHostEntityIndex,
                            int proxyFilePortEntityIndex,
                            int proxyFileLoginIndex,
                            int proxyFilePasswordIndex)
        {
            _f = new FilesWriter();
            _l = _logger;
            ProxyFile = proxyFile;
            ProxyFileLineDelimiter = proxyFileLineDelimiter;
            ProxyFileEntitiesSeparator = proxyFileEntitiesSeparator;
            ProxyFileHostEntityIndex = proxyFileHostEntityIndex;
            ProxyFilePortEntityIndex = proxyFilePortEntityIndex;
            ProxyFileLoginIndex = proxyFileLoginIndex;
            ProxyFilePasswordIndex = proxyFilePasswordIndex;
        }
        public void LaunchProxyesChecking()
        {
            ServiceStatus = BaseServicesStatuses.ServiceLaunching;
            ParseAdresses();
            if (ReferenceEquals(RequestProxyes, null)
                || RequestProxyes.Count.Equals(0))
            {
                _l.warn($"Any proxies scrapped from file {ProxyFile}");
                return;
            }
            ConvertProxies();
            if (ReferenceEquals(ProxyesList, null)
                || ProxyesList.Count.Equals(0))
            {
                _l.warn($"Any converted proxies detected");
                return;
            }
            // CheckProxyState(ProxyesList.First());
            ProxyesList.AsParallel().WithExecutionMode(ParallelExecutionMode.ForceParallelism).ForAll(proxy => UpdateProxieAddressState(ref proxy));
        }
        protected void ParseAdresses()
        {
            RequestProxyes = new List<string>();
            if (ReferenceEquals(ProxyFile, null))
            {
                _l.warn("Proxy service: proxy file is not installed");
                return;
            }
            if (!_f.IsFileExists(ProxyFile))
            {
                _l.warn("Proxy service: proxy file not found");
                return;
            }
            string content = _f.Read(ProxyFile);
            if (!ReferenceEquals(content, String.Empty))
            {
                var _t = content.Split(ProxyFileLineDelimiter);
                if (_t.Length > 0)
                {
                    RequestProxyes = _t.ToList();
                }
            }
        }
        protected void ConvertProxies()
        {
            if (ReferenceEquals(ProxyFileEntitiesSeparator, null)
                || ProxyFileEntitiesSeparator.Equals(String.Empty))
            {
                _l.warn("Proxy service: empty or invalid entities delimiter");
                return;
            }
            try
            {
                ProxyesList = new List<WebScrapperBaseProxyEntity>();
                foreach(string l in RequestProxyes)
                {
                    string[] _t = l.Split(ProxyFileEntitiesSeparator);
                    if (!ReferenceEquals(_t, null)
                        && _t.Length > 0)
                    {
                        var Proxy = new WebScrapperBaseProxyEntity();
                        Proxy.IsProxyAvailable = false;
                        if (ProxyFileHostEntityIndex > -1 && !ReferenceEquals(_t[ProxyFileHostEntityIndex], null))
                        {
                            Proxy.ProxyUrl = _t[ProxyFileHostEntityIndex];
                        }
                        if (ProxyFilePortEntityIndex > -1 && !ReferenceEquals(_t[ProxyFilePortEntityIndex], null))
                        {
                            Proxy.ProxyPort = int.Parse(_t[ProxyFilePortEntityIndex]);
                        }
                        if (ProxyFileLoginIndex > -1 && !ReferenceEquals(_t[ProxyFileLoginIndex], null))
                        {
                            Proxy.AuthLogin = _t[ProxyFileLoginIndex];
                        }
                        if (ProxyFilePasswordIndex > -1 && !ReferenceEquals(_t[ProxyFilePasswordIndex], null))
                        {
                            Proxy.AuthPassword = _t[ProxyFilePasswordIndex];
                        }
                        ProxyesList.Add(Proxy);
                    }
                }
            }
            catch (Exception e)
            {
                _l.error($"Proxy service: some errors occured, during converting proxies to the application entities: {e.Message} -> {e.StackTrace}");
            }
        }
        protected void UpdateProxieAddressState(ref WebScrapperBaseProxyEntity proxy)
        {
            proxy = CheckProxyState(proxy);
        }
        protected WebScrapperBaseProxyEntity CheckProxyState(WebScrapperBaseProxyEntity proxy)
        {
            const string NCSI_TEST_URL = "http://www.msftncsi.com/ncsi.txt";
            const string NCSI_TEST_RESULT = "Microsoft NCSI";
            const string NCSI_DNS = "dns.msftncsi.com";
            const string NCSI_DNS_IP_ADDRESS = "131.107.255.255";

            try
            {
                using (var _wc = new WebClient())
                {
                    var CurrentProxy = new WebProxy(proxy.ProxyUrl, proxy.ProxyPort);
                    CurrentProxy.Credentials = new NetworkCredential(proxy.AuthLogin, proxy.AuthPassword);
                    _wc.Proxy = CurrentProxy;
                    var _ts = _wc.DownloadString(NCSI_TEST_URL);
                    if (_ts != NCSI_TEST_RESULT)
                    {
                        proxy.IsProxyAvailable = false;
                        return proxy;
                    }
                    var DnsHost = Dns.GetHostEntry(NCSI_DNS);
                    if (DnsHost.AddressList.Length <= 0 || DnsHost.AddressList[0].ToString() != NCSI_DNS_IP_ADDRESS)
                    {
                        proxy.IsProxyAvailable = false;
                        return proxy;
                    }
                    proxy.IsProxyAvailable = true;
                    CheckProxiesInvoker();
                    return proxy;
                }
            }
            catch (WebException)
            {
                _l.warn($"Proxy [{proxy.ProxyUrl}:{proxy.ProxyPort}] are not stable, skipping");
                proxy.IsProxyAvailable = false;
                return proxy;
            }
            catch (System.Net.Sockets.SocketException)
            {
                _l.warn($"Proxy [{proxy.ProxyUrl}:{proxy.ProxyPort}] are not stable, skipping");
                proxy.IsProxyAvailable = false;
                return proxy;
            }
        }
        public event ProxyCallback OnProxyCallback;
        protected void CheckProxiesInvoker()
        {
            var a = ProxyesList.Where(p => p.IsProxyAvailable.Equals(true)).ToList();
            if (a.Count > 100)
            {
                ServiceStatus = BaseServicesStatuses.ServiceLaunched;
            }
            else 
            {
                ServiceStatus = BaseServicesStatuses.ServiceLaunching;
            }
            OnProxyCallback?.Invoke(this);
        }
        public WebScrapperBaseProxyEntity GetRandomProxy()
        {
            var _cl = GetActiveProxies();
            if (_cl.Count.Equals(0))
            {
                return null;
            }
            Random _r = new Random();
            int _i = _r.Next(_cl.Count);
            return ProxyesList[_i];
        }
        public List<WebScrapperBaseProxyEntity> GetRandomProxy(int len)
        {
            return ProxyesList;
        }
        protected List<WebScrapperBaseProxyEntity> GetActiveProxies()
        {
            if (ReferenceEquals(ProxyesList, null) || ProxyesList.Count.Equals(0))
            {
                return new List<WebScrapperBaseProxyEntity>();
            }
            var _t = new List<WebScrapperBaseProxyEntity>();
            foreach (var _p in ProxyesList)
            {
                if (_p.IsProxyAvailable)
                {
                    _t.Add(_p);
                }
            }
            if (_t.Count.Equals(0))
            {
                return new List<WebScrapperBaseProxyEntity>();
            }
            return _t.ToList();
        }
        public int GetCurrentProxyesCount()
        {
            return (!ReferenceEquals(ProxyesList, null)) ? ProxyesList.Count : 0;
        }
        public BaseServicesStatuses GetServiceStatus()
        {
            return (!ReferenceEquals(ServiceStatus, null)) ? ServiceStatus : BaseServicesStatuses.ServiceError;
        }
    } 
}