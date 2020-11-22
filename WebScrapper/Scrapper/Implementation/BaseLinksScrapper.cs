using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ScrapySharp.Network;
using ScrapySharp.Extensions;

using HtmlAgilityPack;

using WebApplication.Scrapper.Abstraction;
using WebApplication.Scrapper.Delegates;
using WebApplication.Scrapper.Services;
using WebApplication.Scrapper.Entities;

using WebApplication.Scrapper.Services.Akeneo;
using WebApplication.Scrapper.Services.Akeneo.Delegates;
using WebApplication.Scrapper.Services.Akeneo.Entities;

using WebScrapper.Scrapper.Services.Shopify.Abstraction;

using WebScrapper.Scrapper.Delegates;
using WebScrapper.Scrapper.Abstraction;
using WebScrapper.Scrapper.Entities;
using WebScrapper.Scrapper.Entities.enums;
using WebScrapper.Scrapper.Services;

namespace WebApplication.Scrapper.Implementation {
    public class BaseLinksScrapper : AbstractLinksScrapper {
        private WebScrapperBaseSiteEntity requestScrappingSite { get; set; }
        protected override List<string> LinksPool { get; set; } // Global links pool
        protected override List<string> IndexedLinks { get; set; } // Products list pool
        protected List<string> TurnableLinksList { get; set; } // Processing list pool
        protected override IBaseValidationService ValidationService { get; set;}
        protected override BaseLogger _l { get; set; }
        private AkeneoBaseWriter Akeneo { get; set; }
        private IBaseShopifyProcessor Shopify { get; set; }
        private List<IBaseService> Services { get; set; }
        private WebScrapperBaseStatuses InstanceStatus { get; set; }
        public BaseLinksScrapper(WebScrapperBaseSiteEntity requestScrapperSettings, 
                                BaseLogger logger, 
                                AkeneoBaseWriter akeneo,
                                IBaseShopifyProcessor shopifyProcessor) {

            Akeneo = akeneo;
            Akeneo.OnProductListeningFinished += OnServiceCallback;

            Shopify = shopifyProcessor;
            Shopify.OnShopifyIndexationFinished += OnServiceCallback;

            Services = new List<IBaseService>();
            Services.Add(Shopify);
            Services.Add(Akeneo);

            ValidationService = new WebScrapperLinksValidator();

            InstanceStatus = WebScrapperBaseStatuses.InstanceNotLaunched;

            _l = logger;
            requestScrappingSite = requestScrapperSettings;

            LinksPool = new List<string>();
            IndexedLinks = new List<string>();
            TurnableLinksList = new List<string>();
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
                        LinksScrapperInstance();
                        InstanceStatus = WebScrapperBaseStatuses.InstanceLaunching;
                    }
                }
            }
            _l.info("Cancel, not all service are in finished state!");
        }

        public override void LinksScrapperInstance() {
            if (!ReferenceEquals(requestScrappingSite.CollectionsProcessor, null))
            {
                ICollectionsProcessorService _c = new CollectionsProcessorService(_l);
                var temp = _c.FindCollections(requestScrappingSite.CollectionsProcessor);
                if (!ReferenceEquals(temp.Collections, null))
                {
                    requestScrappingSite.CollectionsProcessor = temp;
                }
            }
            _l.info($"Starting base instance : {requestScrappingSite.ItemUrl}");
            new Thread(() => {
                ValidationService.SetBaseSiteUrl(requestScrappingSite.BaseSiteUrl);
                if (!ValidationService.Validate(requestScrappingSite.BaseSiteUrl)) {
                    _l.warn(ValidationService.GetExceptMessage());
                    return;
                }
                InvokeOnInstanceStatusUpdating(WebScrapperBaseStatuses.InstanceLaunched);
                
                LinksScrapperThread(requestScrappingSite.BaseSiteUrl);
                LinksProcessor();
            }).Start();
        }
        private void LinksProcessor() {
            if (TurnableLinksList.Count > 0) {
                var currentTurnableList = TurnableLinksList;
                var removeList = new List<string>();

                var MathRandom = new Random();

                int OperationsLimitPerMoment = MathRandom.Next(requestScrappingSite.SiteBaseRequestsIntervalMin, requestScrappingSite.SiteBaseRequestsIntervalMax);
                int CurrentOperationNumber = 0;

                for (var i = 0; i <= currentTurnableList.Count() - 1; i++) {
                    string link = currentTurnableList.ElementAt(i);
                    _l.info($"Current link: {link}");
                    ScrapingBrowser _b = new ScrapingBrowser();
                    try {
                        if (CurrentOperationNumber >= OperationsLimitPerMoment) {
                            Thread.Sleep(MathRandom.Next(requestScrappingSite.SiteBaseRequestsIntervalMin * 1000, requestScrappingSite.SiteBaseRequestsIntervalMax * 1000));
                            OperationsLimitPerMoment = MathRandom.Next(requestScrappingSite.SiteBaseRequestsIntervalMin, requestScrappingSite.SiteBaseRequestsIntervalMax);
                            CurrentOperationNumber = 0;
                        }
                        CurrentOperationNumber++;
                        var htmlNode = _b.NavigateToPage(new Uri(link)).Html;
                        if (!ReferenceEquals(htmlNode, null)) {
                            try {
                                if (!ReferenceEquals(requestScrappingSite.SiteProductPageIndicationSelector, null) 
                                    && !requestScrappingSite.SiteProductPageIndicationSelector.Equals(String.Empty)) 
                                {
                                    var productNameNode = SelectNode(requestScrappingSite.SiteProductPageIndicationSelector, htmlNode);
                                      if (!ReferenceEquals(productNameNode, null)) {
                                        var productName = productNameNode.InnerText;
                                        if (!ReferenceEquals(productName.Trim(), String.Empty)) {
                                            IndexedLinks.Add(link);
                                            _l.info($"Adding {link} to products list collection");
                                            ScrapProductFromUrl(link, htmlNode, new WebScrapperBaseProxyEntity {});
                                        }
                                    }
                                }
                                LinksScrapperThread(link);
                            } catch (Exception e) {
                                _l.error($"Product name node selector error : {e.Message}");
                            }
                        }  
                    } catch (Exception e) {
                        _l.error($"Internal scrapping browser error: {e.Message}");
                    }
                    removeList.Add(link);
                }
                if (removeList.Count > 0) {
                    foreach (var item in removeList) {
                        TurnableLinksList.Remove(item);
                    }
                }
            }
            _l.info($"Task {Thread.CurrentThread.Name} : {TurnableLinksList.Count} left in scrapping");
            if (TurnableLinksList.Count > 0) {
                LinksProcessor();
            } else {
                InvokeOnInstanceStatusUpdating(WebScrapperBaseStatuses.InstanceShuttedDown);
                _l.info($"Task {Thread.CurrentThread.Name} finished!");
            }
        }
        protected override void LinksScrapperThread(string l) {
            _l.info("Processor thread");
            if (ValidationService.Validate(l)) {
                ScrapingBrowser _b = new ScrapingBrowser();
                try {
                    var htmlNode = _b.NavigateToPage(new Uri(l)).Html;                
                    if (!ReferenceEquals(htmlNode, null)) {
                        //var links = htmlNode.SelectNodes("//body//a/@href");
                        var links = htmlNode.CssSelect("a");
                        if (!ReferenceEquals(links, null)) {
                            try {
                                var productNameNode = htmlNode.CssSelect(requestScrappingSite.SiteProductPageIndicationSelector);
                                if (!ReferenceEquals(productNameNode, null) && !ReferenceEquals(productNameNode.First(), null)) {
                                    var preparedLink = PrepareLink(l);
                                    _l.info($"Link {preparedLink} is a valid link, adding it to collection!");
                                    LinksPool.Add(preparedLink);
                                } else {
                                    _l.warn("Node selection error: not a product name");
                                }
                            } catch (Exception) {
                                //  _l.warn($"Url {l} is not a valid product page, skip it");
                            }
                            foreach (var link in links) {
                                var linkValue = link.GetAttributeValue("href", "").Trim();
                                if (ValidationService.Validate(linkValue)) {
                                    var preparedLink = PrepareLink(linkValue);
                                    if (!IsLinkExist(preparedLink) && IsNotExcluded(preparedLink)) {
                                        LinksPool.Add(preparedLink);
                                        TurnableLinksList.Add(preparedLink);
                                    } else {
                                        //  _l.warn($"Link {preparedLink} already scrapped, skip it...");
                                    }
                                } else {
                                    //  _l.warn($"Link {linkValue} is not our required link!");
                                }
                            }
                        } else {
                            _l.warn($"Any links on the page {l}");
                        }
                    } else {
                        _l.warn($"Nothing to scrap from url {l}");
                    }
                } catch (AggregateException e) {
                    _l.error(String.Concat(e.Message ," -> " , l));
                }
            } else {
                if (ReferenceEquals(ValidationService.GetExceptMessage(), null)) {
                    // _l.warn($"Link {l} already processed, skipping it...");
                } else {
                    _l.warn(ValidationService.GetExceptMessage());
                }
            }
        }

        protected override string PrepareLink(string link) {
            var BaseUrl = requestScrappingSite.BaseSiteUrl;
            if (link.Contains(BaseUrl)) {
                return link;
            }
            if (link.IndexOf("/").Equals(0)) {
                var t = link[0].ToString();
                if (link[0].ToString().Equals("/") && BaseUrl[BaseUrl.Length - 1].ToString().Equals("/")) {
                    return String.Concat(BaseUrl, link.Substring(1));
                } else if ((!BaseUrl[0].Equals("/") && link[link.Length - 1].Equals("/")) ||
                            (BaseUrl[0].Equals("/") && !link[link.Length - 1].Equals("/")) ) {
                    return String.Concat(BaseUrl, link.Substring(1));
                } else {
                    return String.Concat(BaseUrl, link);
                }
            } 
            return String.Concat(BaseUrl, link);
        }

        protected override bool IsLinkExist(string l) {
            if (ReferenceEquals(LinksPool, null) || LinksPool.Count.Equals(0)) {
                return false;
            }
            try {
                var link = LinksPool.Where(e => e.Equals(l)).FirstOrDefault();
                if (!ReferenceEquals(link, null)) {
                    return true;
                }
                return false;
            } catch (Exception) {
                return false;
            }
        }

        protected bool IsNotExcluded(string link) {
            if (ReferenceEquals(requestScrappingSite.ExcludeUrlsByParts, null) || requestScrappingSite.ExcludeUrlsByParts.Count.Equals(0)) {
                return true;
            }
            if (link.Equals(requestScrappingSite.BaseSiteUrl)) {
                return false;
            }
            if (!ReferenceEquals(requestScrappingSite.ExcludeUrlsByParts, null) && requestScrappingSite.ExcludeUrlsByParts.Count > 0)
            {
                foreach (var excluded in requestScrappingSite.ExcludeUrlsByParts) {
                    if (link.Contains(excluded)) {
                        return false;
                    }
                }
            }
            return true;
        }

        protected override bool IsLinkIndexed(string l) {
            if (ReferenceEquals(IndexedLinks, null) || IndexedLinks.Count.Equals(0)) {
                return false;
            }
            try {
                var link = IndexedLinks.Where(e => e.Equals(l)).FirstOrDefault();
                if (!ReferenceEquals(link, null)) {
                    return true;
                }
                return false;
            } catch (Exception) {
                return false;
            }
        }

        protected HtmlNode SelectNode(string selector, HtmlNode _baseNode) 
        {
            if (selector.Contains(".") || selector.Contains("#") || !selector.Contains("/"))
            {
                return _baseNode.CssSelect(selector).FirstOrDefault();
            }
            return _baseNode.SelectSingleNode(selector);
        }

        protected override void ScrapProductFromUrl(string url, HtmlNode htmlNode, WebScrapperBaseProxyEntity proxyInfo) {
            if (!ReferenceEquals(htmlNode, null)) {
                new Thread(() => {
                    try {
                        var scrapper = new BaseScrapper(htmlNode, proxyInfo, _l, url);
                        AkeneoProduct product = scrapper.ScrappingInstance(requestScrappingSite);
                        if ((ReferenceEquals(product.productCategory, null)) 
                            && !ReferenceEquals(requestScrappingSite.CollectionsProcessor, null)
                            && !ReferenceEquals(requestScrappingSite.CollectionsProcessor.Collections, null)
                            && requestScrappingSite.CollectionsProcessor.Collections.Count > 0)
                        {
                            List<string> firstLevelMatches = new List<string>();
                            foreach (string collection in requestScrappingSite.CollectionsProcessor.Collections)
                            {
                                var c = collection.Trim().Remove(collection.Trim().Length -1);
                                if (product.productName.Contains(c))
                                {
                                    firstLevelMatches.Add(c);
                                }
                            }
                            int MaxCountOfMatches = 0;
                            int CurrentCountOfMatches = 0;
                            foreach (var collection in firstLevelMatches)
                            {
                                CurrentCountOfMatches = 0;
                                var parts = collection.Split(" ").ToList();
                                foreach (var part in parts) 
                                {
                                    if (product.productName.Contains(part))
                                    {
                                        CurrentCountOfMatches++;
                                    }
                                }
                                if (CurrentCountOfMatches > MaxCountOfMatches)
                                {
                                    product.productCategory = collection;
                                    MaxCountOfMatches = CurrentCountOfMatches;
                                }
                            }
                            if (ReferenceEquals(product.productCategory, null))
                            {
                                var d = product.productName.Split(" ");
                                product.productCategory = String.Concat(d[d.Length - 2], " ", d[d.Length - 1]);
                            }
                        }
                        if (!product.isProductInStock)
                        {
                            Shopify.UpdateProduct(product.productName);
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
                }).Start();
            }
        }

        private void InvokeOnInstanceStatusUpdating(WebScrapperBaseStatuses status)
        {
            OnInstanceStatusUpdating?.Invoke(this, new BaseScrapperChangeStatusCallbackResult(status, requestScrappingSite.BaseSiteUrl));
        }
        public override event BaseScrapperChangeStatusCallback OnInstanceStatusUpdating;

        public override void Dispose() {
            LinksPool = new List<string>();
            Akeneo.OnProductListeningFinished -= OnServiceCallback;
            Shopify.OnShopifyIndexationFinished -= OnServiceCallback;
        }
    }
}