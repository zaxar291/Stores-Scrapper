using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using System.Threading;
using System.Net;

using WebScrapper.Scrapper.Abstraction;
using WebScrapper.Scrapper.Delegates;
using WebScrapper.Scrapper.Entities;

using WebScrapper.Scrapper.Services.Shopify;
using WebScrapper.Scrapper.Services.Shopify.Abstraction;
using WebScrapper.Scrapper.Services.Shopify.Delegates;
using WebScrapper.Scrapper.Entities.enums;
using WebScrapper.Scrapper.Services;

using WebApplication.Scrapper.Implementation;
using WebApplication.Scrapper.Abstraction;
using WebApplication.Scrapper.Services.Akeneo.Delegates;
using WebApplication.Scrapper.Services.Akeneo;
using WebApplication.Scrapper.Services;
using WebApplication.Scrapper.Delegates;
using WebApplication.Scrapper.Entities;

using WebApplication.Scrapper.Services.Akeneo.Entities;

using ScrapySharp.Extensions;
using ScrapySharp.Network;
using HtmlAgilityPack;

namespace WebScrapper.Scrapper.Implementation 
{
    public class ShopifyLinksScrapper : AbstractLinksScrapper 
    {
        private List<string> linksPool;
        private string filename;
        private FilesWriter _writer;
        private WebScrapperBaseSiteEntity requestScrappingSite { get; set; }
        protected override List<string> LinksPool { get ;set; }  
        protected override List<string> IndexedLinks { get; set; }
        protected override IBaseValidationService ValidationService { get; set;}
        protected override BaseLogger _l { get; set; }
        private AkeneoBaseWriter Akeneo { get; set; }
        private IBaseShopifyProcessor Shopify { get; set; }
        private List<IBaseService> Services { get; set; }
        private IBaseProxyService _ps { get; set; }
        private WebScrapperBaseStatuses InstanceStatus { get; set; }
        private ShareAsaleService ShareSale;
        public ShopifyLinksScrapper(WebScrapperBaseSiteEntity requestScrapperSettings, 
                                    BaseLogger logger, 
                                    AkeneoBaseWriter akeneo,
                                    IBaseShopifyProcessor shopifyProcessor,
                                    IBaseProxyService ps,
                                    ShareAsaleService _s) 
        {
            _ps = ps;
            _ps.OnProxyCallback += OnProxyServiceCallback;

            ShareSale = _s;

            Services = new List<IBaseService>();
            requestScrappingSite = requestScrapperSettings;
            _l = logger;
            _writer = new FilesWriter();

            Akeneo = akeneo;
            Akeneo.OnProductListeningFinished += OnServiceCallback;

            Shopify = shopifyProcessor;
            Shopify.OnShopifyIndexationFinished += OnServiceCallback;

            InstanceStatus = WebScrapperBaseStatuses.InstanceNotLaunched;
            
            Services.Add(shopifyProcessor);
            Services.Add(akeneo);

            GenerateSitemapFileName();

            linksPool = new List<string>();
            InvokeOnInstanceStatusUpdating(WebScrapperBaseStatuses.InstanceLaunching);
        }

        public void OnServiceCallback(object sender, BaseServiceResponse eventArgs)
        {
            _l.info("Callback from one of the service, checking services statuses...");
            if (!ReferenceEquals(Services, null) && Services.Count > 0)
            {
                var checkList = new List<BaseServicesStatuses>();
                foreach (var Service in Services)
                {
                    checkList.Add(Service.GetServiceStatus());
                }
                if (checkList.All(e => e.Equals(BaseServicesStatuses.ServiceLaunched)))
                {
                    if (InstanceStatus.Equals(WebScrapperBaseStatuses.InstanceNotLaunched))
                    {
                        Akeneo.OnProductListeningFinished -= OnServiceCallback;
                        Shopify.OnShopifyIndexationFinished -= OnServiceCallback;
                        if (_ps.GetServiceStatus().Equals(BaseServicesStatuses.ServiceLaunched))
                        {
                            LinksScrapperInstance();
                            InstanceStatus = WebScrapperBaseStatuses.InstanceLaunching;
                            _ps.OnProxyCallback -= OnProxyServiceCallback;
                        }
                    }
                }
            }
            else 
            {
                _l.info("Cancel, not all service are in finished state!");
            }
        }

        public void OnProxyServiceCallback(object sender)
        {
            _l.info("Callback proxy service");
            if (!ReferenceEquals(Services, null) && Services.Count > 0)
            {
                var checkList = new List<BaseServicesStatuses>();
                foreach (var Service in Services)
                {
                    checkList.Add(Service.GetServiceStatus());
                }
                if (checkList.All(e => e.Equals(BaseServicesStatuses.ServiceLaunched)))
                {
                    if (InstanceStatus.Equals(WebScrapperBaseStatuses.InstanceNotLaunched))
                    {
                        Akeneo.OnProductListeningFinished -= OnServiceCallback;
                        Shopify.OnShopifyIndexationFinished -= OnServiceCallback;
                        if (_ps.GetServiceStatus().Equals(BaseServicesStatuses.ServiceLaunched))
                        {
                            LinksScrapperInstance();
                            InstanceStatus = WebScrapperBaseStatuses.InstanceLaunching;
                            _ps.OnProxyCallback -= OnProxyServiceCallback;
                        }
                    }
                }
            }
            else 
            {
                _l.info("Cancel, not all service are in finished state!");
            }
        }

        public override void LinksScrapperInstance() 
        {
            _l.info($"Starting shopify instance : {requestScrappingSite.ItemUrl}");
            new Thread(() => {
                InvokeOnInstanceStatusUpdating(WebScrapperBaseStatuses.InstanceLaunched);
                ScrapXml();
                ProcessXml();
                if (!ReferenceEquals(linksPool, null) && linksPool.Count > 0) {
                    Random randInteger = new Random();
                    int currentRandomParses = randInteger.Next(requestScrappingSite.SiteBaseRequestsPerSecondMin, requestScrappingSite.SiteBaseRequestsPerSecondMax);
                    int currentThreadTimeOut = randInteger.Next(requestScrappingSite.SiteBaseRequestsIntervalMin, requestScrappingSite.SiteBaseRequestsIntervalMax);
                    int currentLinkNumber = 0;
                    lock (linksPool)
                    {
                        foreach (string url in linksPool.ToList()) 
                        {
                            if (IsUrlExcluded(url))
                            {
                                continue;
                            }
                            if (currentLinkNumber >= currentRandomParses)
                            {
                                _l.info("Sleeping, limit occured");
                                Thread.Sleep(currentThreadTimeOut);
                                currentRandomParses = randInteger.Next(requestScrappingSite.SiteBaseRequestsPerSecondMin, requestScrappingSite.SiteBaseRequestsPerSecondMax);
                                currentThreadTimeOut = randInteger.Next(requestScrappingSite.SiteBaseRequestsIntervalMin * 1000, requestScrappingSite.SiteBaseRequestsIntervalMax * 1000);
                                currentLinkNumber = 0;
                                // break;
                            }
                            if (url != null && !url.Equals(String.Empty) && !url.Equals(requestScrappingSite.BaseSiteUrl))
                            {
                                //LinksScrapperThread(url);
                                new Thread(() => {
                                    LinksScrapperThread(url);
                                }).Start();
                                currentLinkNumber++;
                            }
                        }
                    }
                    InvokeOnInstanceStatusUpdating(WebScrapperBaseStatuses.InstanceShuttedDown);
                    _l.info($"Task {requestScrappingSite.ItemUrl} finished with success code.");
                }
            }).Start();
        }
        protected bool IsUrlExcluded(string url)
        {
            if (ReferenceEquals(requestScrappingSite.ExcludeUrlsByParts, null) 
                || requestScrappingSite.ExcludeUrlsByParts.Count.Equals(0))
            {
                return false;
            }
            try
            {
                foreach (var selected in requestScrappingSite.ExcludeUrlsByParts)
                {
                    if (url.Contains(selected))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        protected override void LinksScrapperThread(string l) 
        {
            int safeCount = 5;
            int currentCount = 0;
            bool IsSuccess = false;
            while (currentCount <= safeCount)
            {
                var proxyInfo = _ps.GetRandomProxy();
                var proxy = new WebProxy(proxyInfo.ProxyUrl, proxyInfo.ProxyPort);
                proxy.Credentials = new NetworkCredential(proxyInfo.AuthLogin, proxyInfo.AuthPassword);
                ScrapingBrowser _browser = new ScrapingBrowser();
                _browser.Proxy = proxy;
                try
                {
                    var htmlNode = _browser.NavigateToPage(new Uri(l)).Html;
                    if (htmlNode != null)
                    {
                        ScrapProductFromUrl(l, htmlNode, proxyInfo);
                        IsSuccess = true;
                        currentCount = safeCount + 1;
                    }
                }
                catch (Exception e)
                {
                    _l.warn($"Shopify scrapper: Attention! Cannot estabilish connection to the {l} via proxy [{proxyInfo.ProxyUrl}:{proxyInfo.ProxyPort}] Attempting to reconnect with another proxy, current trying is {currentCount.ToString()}. Reason: {e.Message} -> {e.StackTrace}");
                    currentCount++;
                }
            }
            if (IsSuccess)
            {
                _l.info($"Product from url {l} successfully processed");
            }
            else
            {
                _l.info($"Some errors occured duting processing product with url {l}, check error/warning logs to get more information about this problem.");
            }
        }
        private void ScrapXml() 
        {
            var safeCount = 5;
            var currentCount = 0;
            bool IsSuccess = false;
            while (currentCount <= safeCount)
            {
                var proxyInfo = _ps.GetRandomProxy();
                var proxy = new WebProxy(proxyInfo.ProxyUrl, proxyInfo.ProxyPort);
                _l.info($"Attempt to connect to the {requestScrappingSite.ItemUrl} via proxy [{proxyInfo.ProxyUrl}:{proxyInfo.ProxyPort}], attempt №{currentCount}");
                proxy.Credentials = new NetworkCredential(proxyInfo.AuthLogin, proxyInfo.AuthPassword);
                ScrapingBrowser _browser = new ScrapingBrowser();
                _browser.Proxy = proxy;
                _browser.Headers.Add("User-Agent", "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:77.0) Gecko/20100101 Firefox/77.0");
                _browser.Headers.Add("Content-type", "text/xml; charset=UTF-8");
                try
                {
                    var html = _browser.NavigateToPage(new Uri(requestScrappingSite.ItemUrl)).Html;
                    var dir = Directory.GetCurrentDirectory();
                    if (_writer.CreateAndWrite(html.InnerHtml, $"products_list-{filename}.xml", $"{dir}/temp/").Equals(1))
                    {
                        _l.info("Links parsed");
                        IsSuccess = true;
                        currentCount = safeCount + 1;
                    }
                    else
                    {
                        currentCount = safeCount + 1;
                        _l.error("Some error during parsing process occured!");
                    }
                }
                catch (Exception e)
                {
                    _l.warn($"Shopify instance: Attention! Connection to {requestScrappingSite.ItemUrl} cannot be estabilished via proxy [{proxyInfo.ProxyUrl}:{proxyInfo.ProxyPort}]. Attempting to reconnect with another proxy, current iteration: {currentCount.ToString()}. Reason: {e.Message} -> {e.StackTrace}");
                    currentCount++;
                }
            }
            if (!IsSuccess)
            {
                _l.error($"Shopify scrapper: Scrapping core fatal error! Connecting to the {requestScrappingSite.ItemUrl}, connection can't be estabilished in {safeCount} attempts, instance work impossible. You can find detail information in application error logs");
            }
            else
            {
                _l.info($"Shopify scrapper: successfully downloaded sitemap for url {requestScrappingSite.ItemUrl}");
            }
        }
        private void ProcessXml() 
        {
            var dir = Directory.GetCurrentDirectory();
            if (!File.Exists($"{dir}/temp/products_list-{filename}.xml"))
            {
                _l.warn($"File {dir}/temp/product_list-{filename}.xml doesn't exists!");
                return;
            }
            XmlDocument doc = new XmlDocument();
            doc.Load($"{dir}/temp/products_list-{filename}.xml");
            foreach (XmlNode firstNode in doc.ChildNodes)
            {
                if (firstNode.Name.ToLower() == "urlset")
                {
                    XmlNamespaceManager namespaceManager = new XmlNamespaceManager(doc.NameTable);
                    namespaceManager.AddNamespace("test", firstNode.NamespaceURI);
                    var nodesList = firstNode.ChildNodes;
                    foreach (XmlNode node in nodesList)
                    {
                        var locNode = node.SelectSingleNode("test:loc", namespaceManager);
                        if (locNode != null)
                        {
                            linksPool.Add(locNode.InnerText);
                        } 
                    }
                }
            }
            _writer.Delete($"{Directory.GetCurrentDirectory()}/temp/products_list-{filename}.xml");
        }
        protected override string PrepareLink(string link) 
        {
            return String.Empty;
        }
        protected override bool IsLinkExist(string l) 
        {
            return false;
        }
        protected override bool IsLinkIndexed(string l) 
        {
            return false;
        }
        protected override void ScrapProductFromUrl(string url, HtmlNode HtmlNode, WebScrapperBaseProxyEntity proxyInfo) 
        {
            if (!ReferenceEquals(HtmlNode, null)) {
                var scrapper = new BaseScrapper(HtmlNode, proxyInfo, _l, url);
                try {
                    AkeneoProduct product = scrapper.ScrappingInstance(requestScrappingSite);
                    ShareSale.AddLinkToProcessing(product.productUrl);
                    var delay = 1000;
                    bool shouldGo = false;
                    while (shouldGo)
                    {
                        var _c = ShareSale.GetLinkSolution(product.productUrl);
                        if (!ReferenceEquals(_c, null))
                        {
                            product.productUrl = _c;
                            break;
                        }
                        Thread.Sleep(delay);
                    }
                    if (!ReferenceEquals(product, null)) {
                        Akeneo.ProcessProduct(product);
                    } else {
                        _l.error("Error in creating AkeneoProduct object, you can find more information in application error log");
                    }
                } catch (Exception e) {
                    _l.error($"Cannot create product from url {url} : {e.Message} -> {e.StackTrace}.");
                }
                return;
            }
        }
        public void InvokeOnInstanceStatusUpdating(WebScrapperBaseStatuses status)
        {
            OnInstanceStatusUpdating?.Invoke(this, new BaseScrapperChangeStatusCallbackResult(status, requestScrappingSite.BaseSiteUrl));
        }
        public override event BaseScrapperChangeStatusCallback OnInstanceStatusUpdating;

        private void GenerateSitemapFileName()
        {
            int max = 20;
            int current = 0;
            while (current <= max)
            {
                current++;
                filename = new Random().Next().ToString();
                if (!_writer.IsFileExists(filename))
                {
                    break;
                }
            }
        }

        public override void Dispose() 
        {
            
        }
    }
}