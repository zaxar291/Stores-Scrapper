using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net;

using Newtonsoft.Json;

using WebApplication.Scrapper.Abstraction;
using WebApplication.Scrapper.Entities;
using WebApplication.Scrapper.Entities.enums;
using WebApplication.Scrapper.Entities.StrategiesEntities;
using WebApplication.Scrapper.Services;


using WebApplication.Scrapper.Services.Akeneo;

using WebScrapper.Scrapper.Abstraction;
using WebScrapper.Scrapper.Implementation.Driver;
using WebScrapper.Scrapper.Entities;
using WebScrapper.Scrapper.Entities.StrategiesEntities.Driver;
using WebScrapper.Scrapper.Entities.StrategiesEntities;
using WebScrapper.Scrapper.Entities.enums;
using WebScrapper.Scrapper.Delegates;
using WebScrapper.Scrapper.Implementation;
using WebScrapper.Scrapper.Services.Shopify;
using WebScrapper.Scrapper.Services;
using WebScrapper.Scrapper.Services.Shopify.Entities;
using WebScrapper.Scrapper.Entities.Application;

using ScrapySharp.Network;
using ScrapySharp.Extensions;
using HtmlAgilityPack;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace WebApplication.Scrapper.Implementation
{
    public class WebScrapperInstance : IScrapperInstance
    {
        public ScrapperBaseStates scrapperStatus { get; set; }
        public List<string> processLists { get; set; }
        public LogsWriter _logger { get; set; }
        private List<WebScrapperBaseSiteEntity> BaseRequestList { get; set; }
        private List<Thread> ThreadsPool;
        private AbstractLinksScrapper SiteController { get; set; }
        private AbstractWriter _f { get; set; }
        private List<CollectionsAsignerEntity> CollectionRules { get; set; }
        private List<VendorsAssignerEntity> VendorsRules { get; set; }
        private WebScrapperApplicationSettings settings { get; set; }
        public WebScrapperInstance()
        {
            #region Logger init
            LogsWriterSettings loggerSettings = new LogsWriterSettings();
            loggerSettings.baseLogsDir = Directory.GetCurrentDirectory();
            loggerSettings.DefaultExtension = ".log";
            loggerSettings.writeInfoLogs = true;
            loggerSettings.writeWarningLogs = true;
            loggerSettings.writeErrorLogs = true;
            this._logger = new LogsWriter(loggerSettings);
            #endregion

            this.scrapperStatus = ScrapperBaseStates.ScrapperNotWorks;

            this.BaseRequestList = new List<WebScrapperBaseSiteEntity>();

            _f = new FilesWriter();

            settings = new WebScrapperApplicationSettings
            {
                ApplicationBaseProductFamilyType = "SharpTest",
                ApplicationUseProxyes = false
            };

            // var resolver = new ChromeDriverResolver(new BaseWebDriverStrategy {UseProxy=true}, new WebScrapperBaseProxyEntity {IsProxyAvailable=true,ProxyUrl="zproxy.lum-superproxy.io",ProxyPort=22225,AuthLogin="lum-customer-hl_418a2333-zone-static-ip-158.46.169.76",AuthPassword="xnrct5totpu5"}, "https://2ip.ru/", _logger);
            // resolver.Initialize();
            // resolver.Dispose();
            ScrapperStatus();
            InitCollectionsHandler();
            InitVendorsHandler();
        }

        private void ScrapperStatus()
        {
            var d = Directory.GetCurrentDirectory();
            string content = _f.Read($"{Directory.GetCurrentDirectory()}/Scrapper/Resources/scrapper.status.json");
            if (!ReferenceEquals(content, String.Empty))
            {
                try
                {
                    WebScrapperResourceEntity decoded = JsonConvert.DeserializeObject<WebScrapperResourceEntity>(content);
                    scrapperStatus = decoded.ScrapperStatus;
                }
                catch (Exception e)
                {
                    _logger.error($"Fatal: Configs reading error, scrapper work impossible : {e.Message} -> {e.StackTrace}");
                }
            }
        }

        private void InitCollectionsHandler()
        {
            string fileName = $"{Directory.GetCurrentDirectory()}/Scrapper/Resources/collections.json";
            if (_f.IsFileExists(fileName))
            {
                string _c = _f.Read(fileName);
                try
                {
                    CollectionRules = JsonConvert.DeserializeObject<List<CollectionsAsignerEntity>>(_c);
                }
                catch (Exception e)
                {
                    _logger.warn($"Collections converter: attention, cannot deserialize rules for collections! {e.Message} -> {e.StackTrace}");
                    CollectionRules = new List<CollectionsAsignerEntity>();
                }
            }
            else
            {
                CollectionRules = new List<CollectionsAsignerEntity>();
            }
        }
        private void InitVendorsHandler()
        {
            string fileName = $"{Directory.GetCurrentDirectory()}/Scrapper/Resources/vendors.json";
            if (_f.IsFileExists(fileName))
            {
                string _c = _f.Read(fileName);
                try
                {
                    VendorsRules = JsonConvert.DeserializeObject<List<VendorsAssignerEntity>>(_c);
                }
                catch (Exception e)
                {
                    _logger.warn($"Vendors handler: cannot read rules for vendors: {e.Message} -> {e.StackTrace}");
                    VendorsRules = new List<VendorsAssignerEntity>();
                }
            }
            else
            {
                VendorsRules = new List<VendorsAssignerEntity>();
            }
        }
        public void Boot()
        {
            if (this.scrapperStatus.Equals(ScrapperBaseStates.ScrapperWorks))
            {
                this._logger.info("Cancel - scrapper already launched");
                return;
            }
            ScrapperInitSites();
            var thread = new Thread(() =>
            {
                ScrapperInstance();
            });
            thread.Start();
        }
        public void ScrapperInstance()
        {
            var currentList = new List<WebScrapperBaseSiteEntity>();
            try
            {
                currentList = BaseRequestList.Where(e => e.SiteStatus.Equals(WebScrapperBaseStatuses.InstanceNotLaunched)).ToList();
            }
            catch (Exception)
            {
                _logger.warn($"Scrapper instance: any site doesn't satisfied to status: ScrapperNotInitialized");
                return;
            }
            if (ReferenceEquals(currentList, null) || currentList.Count.Equals(0))
            {
                this._logger
                    .warn($"Cancel - BaseRequestList: any site doesn't satisfied to scrapp.");
                return;
            }
            var AkeneoWriter = new AkeneoBaseWriter(_logger, CollectionRules, VendorsRules);
            var ShopifySettings = new ShopifyBaseProcessorSettings
            {
                ShopifyStoreUrl = "feedtestsite.myshopify.com",
                ShopifyApiProtocol = "https://",
                ShopifyApiKey = "519a5e0be9ac809dec75ab688876dde5",
                ShopifyApiSecret = "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",
                ShopifyApiToken = "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"
            };
            var ShopifyWriter = new ShopifyBaseProcessor(ShopifySettings, _logger);

            IBaseProxyService proxyService = new ProxyService(_logger, $"{Directory.GetCurrentDirectory()}/Scrapper/Resources/proxies.txt", "\n", ":", 0, 1, 2, 3);

            var ShareSaleSettings = new ShareAsaleSettings
            {
                MaxInstancesCount = 5,
                BaseUrl = "https://account.shareasale.com/a-login.cfm",
                RequestPageUrl = "https://account.shareasale.com/a-customproductlink.cfm",
                Login = "dev-artjoker",
                Password = "ef(*K:P=6%`xFb*="
            };

            var ShareSale = new ShareAsaleService(ShareSaleSettings, _logger);

            foreach (var scrapperItem in BaseRequestList)
            {
                if (scrapperItem.Equals(String.Empty))
                {
                    this._logger.info($"{scrapperItem.BaseSiteUrl} is not valid link!");
                }
                else
                {
                    if (scrapperItem.SitePlatform.Equals(WebScrapperSiteTypes.SitePlatformIsShopify))
                    {
                        _logger.info($"{scrapperItem.BaseSiteUrl} -> is on the shopify platform, launching base strategy");
                        new ShopifyLinksScrapper(scrapperItem, _logger, AkeneoWriter, ShopifyWriter, proxyService, ShareSale);
                    }
                    else
                    {
                        _logger.info($"{scrapperItem.BaseSiteUrl} -> is not on the shopify platform, launching base strategy");
                        new BaseLinksScrapper(scrapperItem, _logger, AkeneoWriter, ShopifyWriter);
                    }
                }
            }
            ShareSale.LaunchService();
            AkeneoWriter.LaunchInstance();
            ShopifyWriter.ListProducts();
            proxyService.LaunchProxyesChecking();
        }
        private void ScrapperInitSites()
        {

            #region Driver strategies
            #region Share a sale links generation driver strategy
            var ShareAsaleDriverStrategy = new BaseWebDriverStrategy
            {
                RequestUrl = "https://account.shareasale.com/a-customproductlink.cfm",
                TasksList = new List<BaseWebDriverTaskStrategy>(),
                UseProxy = true,
                LaunchIncognito = true,
                IgnoreCertificateErrors = true,
                DisableInfoBar = true,
                LaunchHeadless = true
            };

            var ShareAsaleDriverAuthUserNameStrategy = new BaseWebDriverTaskStrategy
            {
                TaskType = BaseWebDriverTasksTypes.TaskUpdateFieldData,
                RequestElement = "#username",
                NewValue = "dev-artjoker"
            };

            var ShareAsaleDriverAuthUserPasswordStrategy = new BaseWebDriverTaskStrategy();
            ShareAsaleDriverAuthUserPasswordStrategy.TaskType = BaseWebDriverTasksTypes.TaskUpdateFieldData;
            ShareAsaleDriverAuthUserPasswordStrategy.RequestElement = "#password";
            ShareAsaleDriverAuthUserPasswordStrategy.NewValue = "ef(*K:P=6%`xFb*=";

            var ShareAsaleDriverAuthUserConfirmingStrategy = new BaseWebDriverTaskStrategy();
            ShareAsaleDriverAuthUserConfirmingStrategy.TaskType = BaseWebDriverTasksTypes.TaskExecuteScript;
            ShareAsaleDriverAuthUserConfirmingStrategy.ScriptSource = "document.getElementById('form1').submit()";

            var ShareAsaleDriverAuthUserConfirmingDelayStrategy = new BaseWebDriverTaskStrategy();
            ShareAsaleDriverAuthUserConfirmingDelayStrategy.TaskType = BaseWebDriverTasksTypes.TaskDelayTask;
            ShareAsaleDriverAuthUserConfirmingDelayStrategy.DelayTime = 10000;

            var ShareAsaleDriverSetUrlStrategy = new BaseWebDriverTaskStrategy();
            ShareAsaleDriverSetUrlStrategy.TaskType = BaseWebDriverTasksTypes.TaskUpdateFieldData;
            ShareAsaleDriverSetUrlStrategy.RequestElement = "#destinationURL";
            ShareAsaleDriverSetUrlStrategy.ObtainFromField = "productUrl";

            var ShareAsaleDriverUrlConfirmingStrategy = new BaseWebDriverTaskStrategy();
            ShareAsaleDriverUrlConfirmingStrategy.TaskType = BaseWebDriverTasksTypes.TaskExecuteScript;
            ShareAsaleDriverUrlConfirmingStrategy.ScriptSource = "document.getElementById('buildLinkFrm').children[7].children[0].click()";

            var ShareAsaleDriverUrlNewUrlReceivingStrategy = new BaseWebDriverTaskStrategy();
            ShareAsaleDriverUrlNewUrlReceivingStrategy.ScriptSource = "return getUrl(); function getUrl(){return document.getElementById(\"buildLinkFrm\").children[7].children[0].getAttribute(\"value\");}";
            ShareAsaleDriverUrlNewUrlReceivingStrategy.AssignToField = "productUrl";
            ShareAsaleDriverUrlNewUrlReceivingStrategy.TaskType = BaseWebDriverTasksTypes.TaskGetDataFromPage;
            ShareAsaleDriverUrlNewUrlReceivingStrategy.ScriptSourceType = WebScrapper.Scrapper.Entities.enums.By.StringSource;
            ShareAsaleDriverUrlNewUrlReceivingStrategy.LoadDriverDependencies = false;

            ShareAsaleDriverStrategy.TasksList.Add(ShareAsaleDriverAuthUserNameStrategy);
            ShareAsaleDriverStrategy.TasksList.Add(ShareAsaleDriverAuthUserPasswordStrategy);
            ShareAsaleDriverStrategy.TasksList.Add(ShareAsaleDriverAuthUserConfirmingStrategy);
            ShareAsaleDriverStrategy.TasksList.Add(ShareAsaleDriverAuthUserConfirmingDelayStrategy);
            ShareAsaleDriverStrategy.TasksList.Add(ShareAsaleDriverSetUrlStrategy);
            ShareAsaleDriverStrategy.TasksList.Add(ShareAsaleDriverUrlConfirmingStrategy);
            ShareAsaleDriverAuthUserConfirmingDelayStrategy.DelayTime = 15000;
            ShareAsaleDriverStrategy.TasksList.Add(ShareAsaleDriverAuthUserConfirmingDelayStrategy);
            ShareAsaleDriverStrategy.TasksList.Add(ShareAsaleDriverUrlNewUrlReceivingStrategy);
            #endregion
            #endregion
            #region Dankgeek 
            WebScrapperBaseSiteEntity Dankgeek = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://dankgeek.com/sitemap_products_1.xml?from=1879131587&to=4662096363604",
                BaseSiteUrl = "https://dankgeek.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ExternalHash = "?aff=299",
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 8,
                SiteBaseRequestsPerSecondMax = 15,
                SiteBaseRequestsIntervalMin = 10,
                SiteBaseRequestsIntervalMax = 20
            };

            StrategyHtmlEntity DankgeekProductPhoto = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "//*[@id=\"ProductPhoto\"]/a[1]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute DankgeekProductPhotoLink = new BaseItemStrategyAttribute
            {
                AttributeName = "href",
                AttributeAssingToRule = "imageUrl"
            };

            DankgeekProductPhoto.AttributesList.Add(DankgeekProductPhotoLink);

            StrategyHtmlEntity DankgeekSchema = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyShopifyMetaScriptSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/script[12]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute DankgeekSchemaVendor = new BaseItemStrategyAttribute
            {
                AttributeName = "vendor",
                AttributeAssingToRule = "productVendor"
            };

            BaseItemStrategyAttribute DankgeekSchemaCode = new BaseItemStrategyAttribute
            {
                AttributeName = "sku",
                AttributeAssingToRule = "productCode"
            };

            BaseItemStrategyAttribute DankgeekSchemaType = new BaseItemStrategyAttribute
            {
                AttributeName = "type",
                AttributeAssingToRule = "productCategory"
            };

            DankgeekSchema.AttributesList.Add(DankgeekSchemaVendor);
            DankgeekSchema.AttributesList.Add(DankgeekSchemaCode);
            DankgeekSchema.AttributesList.Add(DankgeekSchemaType);

            StrategyHtmlEntity DankgeekDescription = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".yotpo.bottomLine.reviews",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute DankgeekDescriptionAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "data-description",
                AttributeAssingToRule = "productDescription"
            };

            DankgeekDescription.AttributesList.Add(DankgeekDescriptionAttribute);

            StrategyHtmlEntity DankgeekProductName = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h1.medium-down--hide",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity DankgeekProductPrice = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h4.medium-down--hide",
                AssignEntityTo = "productPrice"
            };

            StrategyHtmlEntity DankgeekOldPrice = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "//*[@id=\"shopify-section-product-page\"]/section/div/div[4]/div/div/div",
                AssignEntityTo = "productSalePrice"
            };

            StrategyHtmlEntity DankgeekInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                AssignEntityTo = "isProductInStock",
                BaseItemSelector = "#AddToCart",
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "Add to Cart",
                    ResultIfPassed = true,
                    ResultIfFailed = false
                }
            };

            Dankgeek.ScrappingElements.Add(DankgeekProductPhoto);
            Dankgeek.ScrappingElements.Add(DankgeekSchema);
            Dankgeek.ScrappingElements.Add(DankgeekDescription);
            Dankgeek.ScrappingElements.Add(DankgeekProductName);
            Dankgeek.ScrappingElements.Add(DankgeekProductPrice);
            Dankgeek.ScrappingElements.Add(DankgeekOldPrice);
            Dankgeek.ScrappingElements.Add(DankgeekInStockSelector);
            // Todo: add collection after it's handler
            // Dankgeek.ImageNodeCollectionSelector = "a.demo-gallery__img--main";

            #endregion
            #region PuffingBird

            WebScrapperBaseSiteEntity PuffingBird = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://puffingbird.com/sitemap_products_1.xml?from=544516931645&to=5345807204511",
                BaseSiteUrl = "https://puffingbird.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ExternalHash = "?aff=22",
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 8,
                SiteBaseRequestsPerSecondMax = 15,
                SiteBaseRequestsIntervalMin = 10,
                SiteBaseRequestsIntervalMax = 20
            };

            StrategyHtmlEntity PuffingBirdImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "//*[@id=\"ProductPhoto\"]/a",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute PuffingBirdImageSelectorLink = new BaseItemStrategyAttribute
            {
                AttributeName = "href",
                AttributeAssingToRule = "imageUrl"
            };

            PuffingBirdImageSelector.AttributesList.Add(PuffingBirdImageSelectorLink);

            StrategyHtmlEntity PuffingBirdSchemaSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyShopifyMetaScriptSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                AttributesList = new List<BaseItemStrategyAttribute>(),
                BaseItemSelector = "/html/head/script[15]"
            };

            BaseItemStrategyAttribute PuffingBirdSchemaSelectorVendor = new BaseItemStrategyAttribute
            {
                AttributeName = "vendor",
                AttributeAssingToRule = "productVendor"
            };

            BaseItemStrategyAttribute PuffingBirdSchemaSelectorCategory = new BaseItemStrategyAttribute
            {
                AttributeName = "type",
                AttributeAssingToRule = "productCategory"
            };

            BaseItemStrategyAttribute PuffingBirdSchemaSelectorCode = new BaseItemStrategyAttribute
            {
                AttributeName = "sku",
                AttributeAssingToRule = "productCode"
            };

            PuffingBirdSchemaSelector.AttributesList.Add(PuffingBirdSchemaSelectorVendor);
            PuffingBirdSchemaSelector.AttributesList.Add(PuffingBirdSchemaSelectorCategory);
            PuffingBirdSchemaSelector.AttributesList.Add(PuffingBirdSchemaSelectorCode);

            StrategyHtmlEntity PuffingBirdDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "div.easytabs-content-holder",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity PuffingBirdPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                AssignEntityTo = "productPrice",
                BaseItemSelector = "span.product-single__price"
            };

            StrategyHtmlEntity PuffingBirdOldPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                AssignEntityTo = "productSalePrice",
                BaseItemSelector = "#ComparePrice-product-template"
            };

            StrategyHtmlEntity PuffingBirdProductNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                AssignEntityTo = "productName",
                BaseItemSelector = "h1.product-single__title"
            };


            StrategyHtmlEntity PuffingBirdInStockSelectorDankgeekInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                AssignEntityTo = "isProductInStock",
                BaseItemSelector = ".btn.btn--full.product-form__cart-submit.btn--secondary-accent",
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "Add to Cart",
                    ResultIfPassed = true,
                    ResultIfFailed = false
                }
            };

            StrategyHtmlEntity PuffingBirdTagsSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyPuffingBirdTags,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/script",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute PuffingBirdTagsSelectorCategory = new BaseItemStrategyAttribute
            {
                AttributeName = "firstCategory",
                AttributeAssingToRule = "productCategory",
            };

            BaseItemStrategyAttribute PuffingBirdTagsSelectorTags = new BaseItemStrategyAttribute
            {
                AttributeName = "productTags",
                AttributeAssingToRule = "productTags"
            };

            PuffingBirdTagsSelector.AttributesList.Add(PuffingBirdTagsSelectorCategory);
            PuffingBirdTagsSelector.AttributesList.Add(PuffingBirdTagsSelectorTags);

            PuffingBird.ScrappingElements.Add(PuffingBirdImageSelector);
            PuffingBird.ScrappingElements.Add(PuffingBirdSchemaSelector);
            PuffingBird.ScrappingElements.Add(PuffingBirdDescriptionSelector);
            PuffingBird.ScrappingElements.Add(PuffingBirdProductNameSelector);
            PuffingBird.ScrappingElements.Add(PuffingBirdPriceSelector);
            PuffingBird.ScrappingElements.Add(PuffingBirdOldPriceSelector);
            PuffingBird.ScrappingElements.Add(PuffingBirdInStockSelectorDankgeekInStockSelector);
            PuffingBird.ScrappingElements.Add(PuffingBirdTagsSelector);

            // PuffingBird.ImageNodeCollectionSelector = "a.product-single__thumbnail.product-single__thumbnail-product-template";

            #endregion
            #region BadassGlass 
            WebScrapperBaseSiteEntity BadassGlass = new WebScrapperBaseSiteEntity
            {
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                ItemUrl = "https://www.badassglass.com/sitemap_products_1.xml?from=79639609353&to=4656318578769",
                BaseSiteUrl = "https://www.badassglass.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ExternalHash = "?aff=139",
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 5,
                SiteBaseRequestsPerSecondMax = 7,
                SiteBaseRequestsIntervalMin = 10,
                SiteBaseRequestsIntervalMax = 20
            };
            StrategyHtmlEntity BadassGlassNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                BaseItemSelector = "h1.product-title",
                AssignEntityTo = "productName",
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
            };

            StrategyHtmlEntity BadassGlassImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                BaseItemSelector = ".product-galley--image-background img",
                AttributesList = new List<BaseItemStrategyAttribute>(),
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection
            };

            BaseItemStrategyAttribute BadassGlassImageSelectorAttributeNode = new BaseItemStrategyAttribute
            {
                AttributeName = "src",
                AttributeAssingToRule = "imageUrl",
            };

            BadassGlassImageSelector.AttributesList.Add(BadassGlassImageSelectorAttributeNode);

            StrategyHtmlEntity BadassGlassScriptShopifySchemaSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyShopifyMetaScriptSelection,
                BaseItemSelector = "/html/head/script[11]",
                AttributesList = new List<BaseItemStrategyAttribute>(),
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection
            };

            BaseItemStrategyAttribute BadassGlassScriptShopifySchemaSelectorVendor = new BaseItemStrategyAttribute
            {
                AttributeName = "vendor",
                AttributeAssingToRule = "productVendor"
            };

            BaseItemStrategyAttribute BadassGlassScriptShopifySchemaSelectorType = new BaseItemStrategyAttribute
            {
                AttributeName = "type",
                AttributeAssingToRule = "productCategory"
            };

            BadassGlassScriptShopifySchemaSelector.AttributesList.Add(BadassGlassScriptShopifySchemaSelectorVendor);
            BadassGlassScriptShopifySchemaSelector.AttributesList.Add(BadassGlassScriptShopifySchemaSelectorType);

            StrategyHtmlEntity BadassGlassDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                BaseItemSelector = ".product-description",
                AssignEntityTo = "productDescription",
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection
            };

            StrategyHtmlEntity BadassGlassProductCodeSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategySchemaOrgSelection,
                AttributesList = new List<BaseItemStrategyAttribute>(),
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "script",
                SelectByIndexFromRange = "36"
            };

            BaseItemStrategyAttribute BadassGlassProductCodeSelectorSku = new BaseItemStrategyAttribute
            {
                AttributeName = "SchemaSku",
                AttributeAssingToRule = "productCode"
            };

            BadassGlassProductCodeSelector.AttributesList.Add(BadassGlassProductCodeSelectorSku);

            StrategyHtmlEntity BadassGlassProductPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                BaseItemSelector = ".price--main .money",
                AssignEntityTo = "productPrice",
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection
            };

            StrategyHtmlEntity BadassGlassOldPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                BaseItemSelector = ".price--compare-at .money",
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                AssignEntityTo = "productSalePrice"
            };

            StrategyHtmlEntity BadassGlassInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                AssignEntityTo = "isProductInStock",
                BaseItemSelector = ".atc-button--text",
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "Add to cart",
                    ResultIfPassed = true,
                    ResultIfFailed = false
                }
            };

            BadassGlass.ScrappingElements.Add(BadassGlassProductCodeSelector);
            BadassGlass.ScrappingElements.Add(BadassGlassNameSelector);
            BadassGlass.ScrappingElements.Add(BadassGlassImageSelector);
            BadassGlass.ScrappingElements.Add(BadassGlassScriptShopifySchemaSelector);
            BadassGlass.ScrappingElements.Add(BadassGlassDescriptionSelector);
            BadassGlass.ScrappingElements.Add(BadassGlassProductPriceSelector);
            BadassGlass.ScrappingElements.Add(BadassGlassOldPriceSelector);
            BadassGlass.ScrappingElements.Add(BadassGlassInStockSelector);

            #endregion
            #region CbdCO
            var CdbCo = new WebScrapperBaseSiteEntity
            {
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsAnother,
                ItemUrl = "https://cbd.co/",
                BaseSiteUrl = "https://cbd.co/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 5,
                SiteBaseRequestsPerSecondMax = 8,
                SiteBaseRequestsIntervalMin = 2,
                SiteBaseRequestsIntervalMax = 6
            };

            StrategyHtmlEntity ProductNameSelector = new StrategyHtmlEntity();
            ProductNameSelector.SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection;
            ProductNameSelector.BaseItemSelector = "h1.productView-title";
            ProductNameSelector.AssignEntityTo = "productName";
            ProductNameSelector.SelectionType = StrategyHtmlSelectionType.StrategySingularSelection;

            StrategyHtmlEntity DescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                BaseItemSelector = ".productView-description-tabContent",
                AssignEntityTo = "productDescription",
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection
            };

            StrategyHtmlEntity ProductPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                BaseItemSelector = ".price--main",
                AssignEntityTo = "productPrice",
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection
            };

            StrategyHtmlEntity ProductStockEntity = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                BaseItemSelector = "/html/head/meta[11]",
                AttributesList = new List<BaseItemStrategyAttribute>(),
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection
            };

            BaseItemStrategyAttribute ProductInStockContainsAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "content",
                AttributeAssingToRule = "isProductInStock",
                AttributeValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "instock",
                    ResultIfPassed = true,
                    ResultIfFailed = false
                }
            };

            ProductStockEntity.AttributesList.Add(ProductInStockContainsAttribute);

            StrategyHtmlEntity ImageNodeSelector = new StrategyHtmlEntity();
            ImageNodeSelector.SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection;
            ImageNodeSelector.BaseItemSelector = ".productView-imageCarousel-main-item a img";
            ImageNodeSelector.AttributesList = new List<BaseItemStrategyAttribute>();
            ImageNodeSelector.SelectionType = StrategyHtmlSelectionType.StrategySingularSelection;

            BaseItemStrategyAttribute SrcAttribute = new BaseItemStrategyAttribute();
            SrcAttribute.AttributeName = "src";
            SrcAttribute.AttributeAssingToRule = "imageUrl";

            ImageNodeSelector.AttributesList.Add(SrcAttribute);

            StrategyHtmlEntity ProductTechSelectors = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                BaseItemSelector = "div.productView",
                AttributesList = new List<BaseItemStrategyAttribute>(),
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection
            };

            BaseItemStrategyAttribute ProductNameAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "data-name",
                AttributeAssingToRule = "productName"
            };
            BaseItemStrategyAttribute ProductCategoryAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "data-product-category",
                AttributeAssingToRule = "productCategory"
            };
            BaseItemStrategyAttribute ProductVendorAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "data-product-brand",
                AttributeAssingToRule = "productVendor"
            };

            StrategyHtmlEntity SchemaEntity = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategySchemaOrgSelection,
                BaseItemSelector = "script",
                SelectionType = StrategyHtmlSelectionType.StrategyMultipleSelection,
                AttributesList = new List<BaseItemStrategyAttribute>(),
                SelectByIndexFromRange = "21"
            };

            BaseItemStrategyAttribute SchemaSku = new BaseItemStrategyAttribute
            {
                AttributeName = "SchemaSku",
                AttributeAssingToRule = "productCode"
            };
            SchemaEntity.AttributesList.Add(SchemaSku);


            ProductTechSelectors.AttributesList.Add(ProductNameAttribute);
            ProductTechSelectors.AttributesList.Add(ProductCategoryAttribute);
            ProductTechSelectors.AttributesList.Add(ProductVendorAttribute);

            CdbCo.ScrappingElements.Add(SchemaEntity);
            CdbCo.ScrappingElements.Add(ProductStockEntity);
            CdbCo.ScrappingElements.Add(ProductPriceSelector);
            CdbCo.ScrappingElements.Add(DescriptionSelector);
            CdbCo.ScrappingElements.Add(ImageNodeSelector);
            CdbCo.ScrappingElements.Add(ProductTechSelectors);
            CdbCo.ScrappingElements.Add(ProductNameSelector);

            CdbCo.ExcludeUrlsByParts = new List<string>();
            CdbCo.ExcludeUrlsByParts.Add("account");
            CdbCo.ExcludeUrlsByParts.Add("login");
            CdbCo.ExcludeUrlsByParts.Add("blog");
            CdbCo.ExcludeUrlsByParts.Add("sort");
            CdbCo.ExcludeUrlsByParts.Add("cart");
            CdbCo.ExcludeUrlsByParts.Add("compare");

            var CdbCoDriverStrategy = new BaseWebDriverStrategy();
            CdbCoDriverStrategy.RequestUrl = "https://account.shareasale.com/a-customproductlink.cfm";
            CdbCoDriverStrategy.TasksList = new List<BaseWebDriverTaskStrategy>();

            var CdbCoDriverAuthUserNameStrategy = new BaseWebDriverTaskStrategy();
            CdbCoDriverAuthUserNameStrategy.TaskType = BaseWebDriverTasksTypes.TaskUpdateFieldData;
            CdbCoDriverAuthUserNameStrategy.RequestElement = "#username";
            CdbCoDriverAuthUserNameStrategy.NewValue = "dev-artjoker";

            var CdbCoDriverAuthUserPasswordStrategy = new BaseWebDriverTaskStrategy();
            CdbCoDriverAuthUserPasswordStrategy.TaskType = BaseWebDriverTasksTypes.TaskUpdateFieldData;
            CdbCoDriverAuthUserPasswordStrategy.RequestElement = "#password";
            CdbCoDriverAuthUserPasswordStrategy.NewValue = "ef(*K:P=6%`xFb*=";

            var CdbCoDriverAuthUserConfirmingStrategy = new BaseWebDriverTaskStrategy();
            CdbCoDriverAuthUserConfirmingStrategy.TaskType = BaseWebDriverTasksTypes.TaskExecuteScript;
            CdbCoDriverAuthUserConfirmingStrategy.ScriptSource = "document.getElementById('form1').submit()";

            var CdbCoDriverAuthUserConfirmingDelayStrategy = new BaseWebDriverTaskStrategy();
            CdbCoDriverAuthUserConfirmingDelayStrategy.TaskType = BaseWebDriverTasksTypes.TaskDelayTask;
            CdbCoDriverAuthUserConfirmingDelayStrategy.DelayTime = 10000;

            var CdbCoDriverSetUrlStrategy = new BaseWebDriverTaskStrategy();
            CdbCoDriverSetUrlStrategy.TaskType = BaseWebDriverTasksTypes.TaskUpdateFieldData;
            CdbCoDriverSetUrlStrategy.RequestElement = "#destinationURL";
            CdbCoDriverSetUrlStrategy.ObtainFromField = "productUrl";

            var CdbCoDriverUrlConfirmingStrategy = new BaseWebDriverTaskStrategy();
            CdbCoDriverUrlConfirmingStrategy.TaskType = BaseWebDriverTasksTypes.TaskExecuteScript;
            CdbCoDriverUrlConfirmingStrategy.ScriptSource = "document.getElementById('buildLinkFrm').children[7].children[0].click()";

            var CdbCoDriverUrlNewUrlReceivingStrategy = new BaseWebDriverTaskStrategy();
            CdbCoDriverUrlNewUrlReceivingStrategy.ScriptSource = "return document.getElementById(\"buildLinkFrm\").children[7].children[0].getAttribute(\"value\")";
            CdbCoDriverUrlNewUrlReceivingStrategy.AssignToField = "productUrl";
            CdbCoDriverUrlNewUrlReceivingStrategy.TaskType = BaseWebDriverTasksTypes.TaskGetDataFromPage;

            CdbCoDriverStrategy.TasksList.Add(CdbCoDriverAuthUserNameStrategy);
            CdbCoDriverStrategy.TasksList.Add(CdbCoDriverAuthUserPasswordStrategy);
            CdbCoDriverStrategy.TasksList.Add(CdbCoDriverAuthUserConfirmingStrategy);
            CdbCoDriverStrategy.TasksList.Add(CdbCoDriverAuthUserConfirmingDelayStrategy);
            CdbCoDriverStrategy.TasksList.Add(CdbCoDriverSetUrlStrategy);
            CdbCoDriverStrategy.TasksList.Add(CdbCoDriverUrlConfirmingStrategy);
            CdbCoDriverAuthUserConfirmingDelayStrategy.DelayTime = 15000;
            CdbCoDriverStrategy.TasksList.Add(CdbCoDriverAuthUserConfirmingDelayStrategy);
            CdbCoDriverStrategy.TasksList.Add(CdbCoDriverUrlNewUrlReceivingStrategy);

            CdbCo.DriverStrategy = CdbCoDriverStrategy;

            CdbCo.SiteProductPageIndicationSelector = ".productView-title";
            #endregion   
            #region TokerSupply
            var TokerSupply = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://www.tokersupply.com/sitemap_products_1.xml?from=1152094849&to=4492427886675",
                BaseSiteUrl = "https://www.tokersupply.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ExternalHash = "?aff=282",
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 5,
                SiteBaseRequestsPerSecondMax = 7,
                SiteBaseRequestsIntervalMin = 10,
                SiteBaseRequestsIntervalMax = 20
            };

            StrategyHtmlEntity TokerSupplyProductName = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h1.product-title",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity TokerSupplyProductShopifySchema = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyShopifyMetaScriptSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/script[9]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute TokerSupplyProductShopifySchemaVendor = new BaseItemStrategyAttribute
            {
                AttributeName = "vendor",
                AttributeAssingToRule = "productVendor"
            };

            BaseItemStrategyAttribute TokerSupplyProductShopifySchemaCategory = new BaseItemStrategyAttribute
            {
                AttributeName = "type",
                AttributeAssingToRule = "productCategory"
            };

            BaseItemStrategyAttribute TokerSupplyProductShopifySchemaCode = new BaseItemStrategyAttribute
            {
                AttributeName = "sku",
                AttributeAssingToRule = "productCode"
            };

            TokerSupplyProductShopifySchema.AttributesList.Add(TokerSupplyProductShopifySchemaVendor);
            TokerSupplyProductShopifySchema.AttributesList.Add(TokerSupplyProductShopifySchemaCategory);
            TokerSupplyProductShopifySchema.AttributesList.Add(TokerSupplyProductShopifySchemaCode);

            StrategyHtmlEntity TokerSupplyProductDescription = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "div.product-description",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity TokerSupplyProductImage = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".product-galley--image-background img",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute TokerSupplyProductImageLink = new BaseItemStrategyAttribute
            {
                AttributeName = "src",
                AttributeAssingToRule = "imageUrl"
            };

            TokerSupplyProductImage.AttributesList.Add(TokerSupplyProductImageLink);

            StrategyHtmlEntity TokerSupplyProductPrice = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".price--main .money",
                AssignEntityTo = "productPrice"
            };

            StrategyHtmlEntity TokerSupplyProductOldPrice = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                BaseItemSelector = ".price--compare-at .money",
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                AssignEntityTo = "productSalePrice"
            };

            StrategyHtmlEntity TokerSupplyProductCodeSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategySchemaOrgSelection,
                AttributesList = new List<BaseItemStrategyAttribute>(),
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "script",
                SelectByIndexFromRange = "36"
            };

            BaseItemStrategyAttribute TokerSupplyProductCodeSelectorSku = new BaseItemStrategyAttribute
            {
                AttributeName = "SchemaSku",
                AttributeAssingToRule = "productCode"
            };

            TokerSupplyProductCodeSelector.AttributesList.Add(TokerSupplyProductCodeSelectorSku);

            StrategyHtmlEntity TokerSupplyScriptShopifySchemaSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyShopifyMetaScriptSelection,
                BaseItemSelector = "/html/head/script[9]",
                AttributesList = new List<BaseItemStrategyAttribute>(),
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection
            };

            BaseItemStrategyAttribute TokerSupplyScriptShopifySchemaSelectorVendor = new BaseItemStrategyAttribute
            {
                AttributeName = "vendor",
                AttributeAssingToRule = "productVendor"
            };

            BaseItemStrategyAttribute TokerSupplyScriptShopifySchemaSelectorType = new BaseItemStrategyAttribute
            {
                AttributeName = "type",
                AttributeAssingToRule = "productCategory"
            };

            TokerSupplyScriptShopifySchemaSelector.AttributesList.Add(TokerSupplyScriptShopifySchemaSelectorVendor);
            TokerSupplyScriptShopifySchemaSelector.AttributesList.Add(TokerSupplyScriptShopifySchemaSelectorType);


            StrategyHtmlEntity TokerSupplyInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                AssignEntityTo = "isProductInStock",
                BaseItemSelector = ".product-form--atc-button .atc-button--text",
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "ADD TO CART",
                    ResultIfPassed = true,
                    ResultIfFailed = false
                }
            };

            TokerSupply.ScrappingElements.Add(TokerSupplyInStockSelector);
            TokerSupply.ScrappingElements.Add(TokerSupplyProductName);
            TokerSupply.ScrappingElements.Add(TokerSupplyProductShopifySchema);
            TokerSupply.ScrappingElements.Add(TokerSupplyProductDescription);
            TokerSupply.ScrappingElements.Add(TokerSupplyProductImage);
            TokerSupply.ScrappingElements.Add(TokerSupplyProductPrice);
            TokerSupply.ScrappingElements.Add(TokerSupplyProductOldPrice);
            TokerSupply.ScrappingElements.Add(TokerSupplyProductCodeSelector);
            TokerSupply.ScrappingElements.Add(TokerSupplyScriptShopifySchemaSelector);

            #endregion
            #region Oozelife
            WebScrapperBaseSiteEntity Oozelife = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://www.oozelife.com/sitemap_products_1.xml?from=6734494726&to=5136789962886",
                BaseSiteUrl = "https://www.oozelife.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ExternalHash = "?ac=weedrepublic&utm_source=oozelife.vwa.la",
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 8,
                SiteBaseRequestsPerSecondMax = 15,
                SiteBaseRequestsIntervalMin = 5,
                SiteBaseRequestsIntervalMax = 7
            };

            StrategyHtmlEntity OozelifeProductName = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "//*[@id=\"AddToCartForm\"]/div[1]/h1/div",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity OozelifeImageUrl = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "a.card__image-container",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute OozelifeImageUrlLink = new BaseItemStrategyAttribute
            {
                AttributeName = "href",
                AttributeAssingToRule = "imageUrl"
            };

            OozelifeImageUrl.AttributesList.Add(OozelifeImageUrlLink);

            StrategyHtmlEntity OozelifeDescription = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "div.description",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity OozelifePrice = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "span#ProductPrice-product-template",
                AssignEntityTo = "productPrice"
            };

            StrategyHtmlEntity OozelifeShopifySchema = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyShopifyMetaScriptSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/script[14]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute OozelifeShopifySchemaVendor = new BaseItemStrategyAttribute
            {
                AttributeName = "vendor",
                AttributeAssingToRule = "productVendor"
            };

            BaseItemStrategyAttribute OozelifeShopifySchemaCategory = new BaseItemStrategyAttribute
            {
                AttributeName = "type",
                AttributeAssingToRule = "productCategory"
            };

            BaseItemStrategyAttribute OozelifeShopifySchemaSku = new BaseItemStrategyAttribute
            {
                AttributeName = "sku",
                AttributeAssingToRule = "productCode"
            };

            OozelifeShopifySchema.AttributesList.Add(OozelifeShopifySchemaVendor);
            OozelifeShopifySchema.AttributesList.Add(OozelifeShopifySchemaCategory);
            OozelifeShopifySchema.AttributesList.Add(OozelifeShopifySchemaSku);

            StrategyHtmlEntity OozelifeInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                AssignEntityTo = "isProductInStock",
                BaseItemSelector = "span#AddToCartText-product-template",
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "Add to Cart",
                    ResultIfPassed = true,
                    ResultIfFailed = false
                }
            };

            StrategyHtmlEntity OozelifeProductTemplate = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyShopifyJsonProductTemplate,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "#ProductJson-product-template",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute BaseItemStrategyAttributeTags = new BaseItemStrategyAttribute
            {
                AttributeName = "PreparedTags",
                AttributeAssingToRule = "productTags"
            };

            OozelifeProductTemplate.AttributesList.Add(BaseItemStrategyAttributeTags);

            Oozelife.ScrappingElements.Add(OozelifeProductName);
            Oozelife.ScrappingElements.Add(OozelifeImageUrl);
            Oozelife.ScrappingElements.Add(OozelifeDescription);
            Oozelife.ScrappingElements.Add(OozelifePrice);
            Oozelife.ScrappingElements.Add(OozelifeShopifySchema);
            Oozelife.ScrappingElements.Add(OozelifeInStockSelector);
            Oozelife.ScrappingElements.Add(OozelifeProductTemplate);

            #endregion
            #region DrDabber

            WebScrapperBaseSiteEntity DrDabber = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://www.drdabber.com/sitemap_products_1.xml?from=6527868995&to=4496366731337",
                BaseSiteUrl = "https://www.drdabber.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ExternalHash = "?rfsn=2724755.554e83&utm_source=refersion&utm_medium=affiliate&utm_campaign=2724755.554e83",
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 5,
                SiteBaseRequestsPerSecondMax = 7,
                SiteBaseRequestsIntervalMin = 10,
                SiteBaseRequestsIntervalMax = 20
            };

            StrategyHtmlEntity DrDabberProductName = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h1.tt-title",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity DrDabberProductUrl = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".11.zoom-product",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute DrDabberProductUrlSrc = new BaseItemStrategyAttribute
            {
                AttributeName = "src",
                AttributeAssingToRule = "imageUrl"
            };

            DrDabberProductUrl.AttributesList.Add(DrDabberProductUrlSrc);

            StrategyHtmlEntity DrDabberProductDescription = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".tt-collapse-content",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity DrDabberProductPrice = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".new-price .money",
                AssignEntityTo = "productPrice"
            };

            StrategyHtmlEntity DrDrabberProductShopifyMeta = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyShopifyMetaScriptSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/script[13]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute DrDrabberProductShopifyMetaVendor = new BaseItemStrategyAttribute
            {
                AttributeName = "vendor",
                AttributeAssingToRule = "productVendor"
            };

            BaseItemStrategyAttribute DrDrabberProductShopifyMetaType = new BaseItemStrategyAttribute
            {
                AttributeName = "type",
                AttributeAssingToRule = "productCategory"
            };

            BaseItemStrategyAttribute DrDrabberProductShopifyMetaCode = new BaseItemStrategyAttribute
            {
                AttributeName = "sku",
                AttributeAssingToRule = "productCode"
            };

            DrDrabberProductShopifyMeta.AttributesList.Add(DrDrabberProductShopifyMetaVendor);
            DrDrabberProductShopifyMeta.AttributesList.Add(DrDrabberProductShopifyMetaType);
            DrDrabberProductShopifyMeta.AttributesList.Add(DrDrabberProductShopifyMetaCode);

            StrategyHtmlEntity DrDrabberInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                AssignEntityTo = "isProductInStock",
                BaseItemSelector = ".btn-addtocart.addtocart-js",
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "ADD TO CART",
                    ResultIfPassed = true,
                    ResultIfFailed = false
                }
            };

            DrDabber.ScrappingElements.Add(DrDabberProductDescription);
            DrDabber.ScrappingElements.Add(DrDabberProductName);
            DrDabber.ScrappingElements.Add(DrDabberProductUrl);
            DrDabber.ScrappingElements.Add(DrDabberProductPrice);
            DrDabber.ScrappingElements.Add(DrDrabberProductShopifyMeta);
            DrDabber.ScrappingElements.Add(DrDrabberInStockSelector);

            #endregion
            #region GrassCity
            WebScrapperBaseSiteEntity GrassSity = new WebScrapperBaseSiteEntity
            {
                BaseSiteUrl = "https://www.grasscity.com/",
                ItemUrl = "https://www.grasscity.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ExternalHash = "?ref=weedrepublic",
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsAnother,
                SiteProductPageIndicationSelector = "div.product-add-form",
                ScrappingElements = new List<StrategyHtmlEntity>(),
                CollectionsProcessor = new WebScrapperBaseCollectionsProcessorEntity
                {
                    IsCategoriesPreparingRequired = true,
                    ProcessingType = WebScrapperBaseCollectionsProcessorEntityProcessingType.ProcessByDriver,
                    DriverSettings = new BaseWebDriverStrategy
                    {
                        RequestUrl = "https://www.grasscity.com/",
                        TasksList = new List<BaseWebDriverTaskStrategy>()
                    },
                    Collections = new List<string>()
                },
                ExcludeUrlsByParts = new List<string>(),
                SiteBaseRequestsPerSecondMin = 5,
                SiteBaseRequestsPerSecondMax = 8,
                SiteBaseRequestsIntervalMin = 2,
                SiteBaseRequestsIntervalMax = 6
            };

            GrassSity.CollectionsProcessor.DriverSettings.TasksList.Add(new BaseWebDriverTaskStrategy
            {
                TaskType = BaseWebDriverTasksTypes.TaskExecuteScript,
                ScriptSource = "scripts/js/TaskGrasscityListCategories.js",
                ScriptSourceType = WebScrapper.Scrapper.Entities.enums.By.FileSource
            });

            StrategyHtmlEntity GrassCityNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "span.base",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity GrassCityImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "#mtImageContainer div a img",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute GrassCityImageSelectorUrl = new BaseItemStrategyAttribute
            {
                AttributeName = "src",
                AttributeAssingToRule = "imageUrl"
            };

            GrassCityImageSelector.AttributesList.Add(GrassCityImageSelectorUrl);

            StrategyHtmlEntity GrassCityDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".product.description",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity GrassCityPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".price-wrapper span.price",
                AssignEntityTo = "productPrice"
            };

            StrategyHtmlEntity GrassCityOldPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "span.old-price .price-container .price-wrapper .price",
                AssignEntityTo = "productSalePrice"
            };

            StrategyHtmlEntity GrassCityInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "#product-addtocart-button",
                AssignEntityTo = "isProductInStock",
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "Add to Cart",
                    ResultIfPassed = true,
                    ResultIfFailed = false
                }
            };

            StrategyHtmlEntity GrassCityJsSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyGrassCityJs,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/body/script[32]",
            };

            StrategyHtmlEntity GrassCitySkuSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "//*[@id=\"product-attribute-specs-table\"]/tbody/tr[1]/td",
                AssignEntityTo = "productCode"
            };


            StrategyHtmlEntity GrassCityVendorSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "//*[@id=\"product-attribute-specs-table\"]/tbody/tr[2]/td",
                AssignEntityTo = "productVendor"
            };

            GrassSity.ScrappingElements.Add(GrassCityNameSelector);
            GrassSity.ScrappingElements.Add(GrassCityImageSelector);
            GrassSity.ScrappingElements.Add(GrassCityDescriptionSelector);
            GrassSity.ScrappingElements.Add(GrassCityPriceSelector);
            GrassSity.ScrappingElements.Add(GrassCityOldPriceSelector);
            GrassSity.ScrappingElements.Add(GrassCityInStockSelector);
            GrassSity.ScrappingElements.Add(GrassCityJsSelector);
            GrassSity.ScrappingElements.Add(GrassCitySkuSelector);
            GrassSity.ScrappingElements.Add(GrassCityVendorSelector);

            GrassSity.ExcludeUrlsByParts.Add("cart");
            GrassSity.ExcludeUrlsByParts.Add("checkout");
            GrassSity.ExcludeUrlsByParts.Add("customer");
            GrassSity.ExcludeUrlsByParts.Add("knowledgebase");
            GrassSity.ExcludeUrlsByParts.Add("catalogsearch");
            GrassSity.ExcludeUrlsByParts.Add("cache");
            GrassSity.ExcludeUrlsByParts.Add(".jpg");
            GrassSity.ExcludeUrlsByParts.Add(".png");
            GrassSity.ExcludeUrlsByParts.Add(".bmp");
            GrassSity.ExcludeUrlsByParts.Add(".jpeg");
            GrassSity.ExcludeUrlsByParts.Add(".svg");
            GrassSity.ExcludeUrlsByParts.Add("order");
            GrassSity.ExcludeUrlsByParts.Add("?price");
            GrassSity.ExcludeUrlsByParts.Add("?brand");
            GrassSity.ExcludeUrlsByParts.Add("?search_color");
            GrassSity.ExcludeUrlsByParts.Add("?choose_glass_thickness");
            GrassSity.ExcludeUrlsByParts.Add("?choose_bong_height");
            GrassSity.ExcludeUrlsByParts.Add("?joint_size");
            GrassSity.ExcludeUrlsByParts.Add("?choose_percolator_type");
            GrassSity.ExcludeUrlsByParts.Add("?am_on_sale");
            GrassSity.ExcludeUrlsByParts.Add("?joint_size");
            GrassSity.ExcludeUrlsByParts.Add("?glass_patterns_techniques");
            GrassSity.ExcludeUrlsByParts.Add("?glass_artists");
            GrassSity.ExcludeUrlsByParts.Add("?rolling_paper_size");
            GrassSity.ExcludeUrlsByParts.Add("?rolling_paper_flavor");
            GrassSity.ExcludeUrlsByParts.Add("?rolling_paper_size");
            GrassSity.ExcludeUrlsByParts.Add("&amp");
            GrassSity.ExcludeUrlsByParts.Add("?amp");
            GrassSity.ExcludeUrlsByParts.Add("?filter");

            #endregion
            #region CbdResellers

            WebScrapperBaseSiteEntity CbdResellers = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://www.cbdresellers.com/",
                BaseSiteUrl = "https://www.cbdresellers.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsAnother,
                SiteProductPageIndicationSelector = "h1.productView-title",
                ScrappingElements = new List<StrategyHtmlEntity>(),
                ExcludeUrlsByParts = new List<string>(),
                DriverStrategy = new BaseWebDriverStrategy
                {
                    RequestUrl = "current",
                    TasksList = new List<BaseWebDriverTaskStrategy>()
                },
                SiteBaseRequestsPerSecondMin = 1,
                SiteBaseRequestsPerSecondMax = 5,
                SiteBaseRequestsIntervalMin = 7,
                SiteBaseRequestsIntervalMax = 15
            };

            StrategyHtmlEntity CbdResellersNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h1.productView-title",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity CbdResellersImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".productView-imageCarousel-main li img",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute CbdResellersImageSelectorLink = new BaseItemStrategyAttribute
            {
                AttributeName = "src",
                AttributeAssingToRule = "imageUrl"
            };

            CbdResellersImageSelector.AttributesList.Add(CbdResellersImageSelectorLink);

            StrategyHtmlEntity CbdResellersDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".productView-description-tabContent",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity CbdResellersPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".price.price--withoutTax.price--main",
                AssignEntityTo = "productPrice"
            };

            StrategyHtmlEntity CbdResellersOldPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".price.price--non-sale",
                AssignEntityTo = "productSalePrice"
            };

            StrategyHtmlEntity CbdResellersSkuSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".productView-info .productView-info-value--sku",
                AssignEntityTo = "productCode"
            };

            StrategyHtmlEntity CbdResellersInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "#form-action-addToCart",
                AssignEntityTo = "isProductInStock",
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Null",
                    ResultIfPassed = false,
                    ResultIfFailed = true
                }
            };

            StrategyHtmlEntity CbdResellersCategorySelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategyMultipleSelection,
                BaseItemSelector = ".breadcrumb",
                SelectByIndexFromRange = "prelast",
                AssignEntityTo = "productCategory"
            };

            StrategyHtmlEntity CbdResellersVendorSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".productView-brand",
                AssignEntityTo = "productVendor"
            };

            CbdResellers.ExcludeUrlsByParts.Add("login");
            CbdResellers.ExcludeUrlsByParts.Add("cart");
            CbdResellers.ExcludeUrlsByParts.Add("checkout");

            CbdResellers.ScrappingElements.Add(CbdResellersNameSelector);
            CbdResellers.ScrappingElements.Add(CbdResellersImageSelector);
            CbdResellers.ScrappingElements.Add(CbdResellersDescriptionSelector);
            CbdResellers.ScrappingElements.Add(CbdResellersPriceSelector);
            CbdResellers.ScrappingElements.Add(CbdResellersOldPriceSelector);
            CbdResellers.ScrappingElements.Add(CbdResellersSkuSelector);
            CbdResellers.ScrappingElements.Add(CbdResellersInStockSelector);
            CbdResellers.ScrappingElements.Add(CbdResellersCategorySelector);
            CbdResellers.ScrappingElements.Add(CbdResellersVendorSelector);

            var CbdResellersDriver = new BaseWebDriverStrategy
            {
                RequestUrl = "--current--",
                TasksList = new List<BaseWebDriverTaskStrategy>()
            };
            var CbdResellersDriverDelayTask = new BaseWebDriverTaskStrategy
            {
                TaskType = BaseWebDriverTasksTypes.TaskDelayTask,
                DelayTime = 10000
            };
            var CbdResellersDriverExecuteCjScriptTask = new BaseWebDriverTaskStrategy
            {
                TaskType = BaseWebDriverTasksTypes.TaskExecuteScript,
                ScriptSource = "document.body.appendChild(document.createElement('script')).src='https://members.cj.com/member/publisherBookmarklet.js?version=1';"
                //ScriptSource = "(function(){document.body.appendChild(document.createElement('script')).src='https://members.cj.com/member/publisherBookmarklet.js?version=1';})();"
            };
            var CbdResellersDriverFrameCheckingTask = new BaseWebDriverTaskStrategy
            {
                TaskType = BaseWebDriverTasksTypes.TaskAwaitTask,
                RequestElement = "#cj-bookmarklet-content",
                AwaitParams = new BaseWebDriverTaskAwaitStrategy
                {
                    AwaitMaxAttempts = 20,
                    AwaitDelayTime = 1000,
                    AwaitCheckingStrategy = BaseWebDriverAwaitTaskAction.SwitchToFrame
                }
            };
            var CbdResellersDriverFieldCheckingTask = new BaseWebDriverTaskStrategy
            {
                TaskType = BaseWebDriverTasksTypes.TaskAwaitTask,
                RequestElement = "#username",
                AwaitParams = new BaseWebDriverTaskAwaitStrategy
                {
                    AwaitMaxAttempts = 20,
                    AwaitDelayTime = 1000,
                    AwaitCheckingStrategy = BaseWebDriverAwaitTaskAction.SelectElement
                }
            };
            var CbdResellersDriverCjAuthExecuteUserNameTask = new BaseWebDriverTaskStrategy
            {
                TaskType = BaseWebDriverTasksTypes.TaskUpdateFieldData,
                RequestElement = "#username",
                NewValue = "z.stadnichenko@artjoker.team"
            };

            var CbdResellersDriverCjAuthExecuteUserPasswordTask = new BaseWebDriverTaskStrategy
            {
                TaskType = BaseWebDriverTasksTypes.TaskUpdateFieldData,
                RequestElement = "#password",
                NewValue = "3v6DmG*L'*;t?g$Q"
            };

            var CbdResellersDriverCjInvokeMemberClickTask = new BaseWebDriverTaskStrategy
            {
                TaskType = BaseWebDriverTasksTypes.TaskExecuteScript,
                ScriptSource = "if(typeof $==undefined){document.body.appendChild(document.createElement(\"script\")).src=\"https://code.jquery.com/jquery-3.5.1.min.js\";} $(\".logInButton\").click();"
            };

            var CbdResellersDriverCjInvokeMemberGetFieldDataTask = new BaseWebDriverTaskStrategy
            {
                TaskType = BaseWebDriverTasksTypes.TaskGetDataFromPage,
                ScriptSource = "if(typeof $==undefined){document.body.appendChild(document.createElement(\"script\")).src=\"https://code.jquery.com/jquery-3.5.1.min.js\";} return $(\".data textarea\").text()",
                AssignToField = "productUrl"
            };

            CbdResellersDriver.TasksList.Add(CbdResellersDriverDelayTask);
            CbdResellersDriver.TasksList.Add(CbdResellersDriverExecuteCjScriptTask);
            CbdResellersDriver.TasksList.Add(CbdResellersDriverFrameCheckingTask);
            CbdResellersDriver.TasksList.Add(CbdResellersDriverFieldCheckingTask);
            CbdResellersDriver.TasksList.Add(CbdResellersDriverCjAuthExecuteUserNameTask);
            CbdResellersDriver.TasksList.Add(CbdResellersDriverCjAuthExecuteUserPasswordTask);
            CbdResellersDriver.TasksList.Add(CbdResellersDriverCjInvokeMemberClickTask);
            CbdResellersDriver.TasksList.Add(CbdResellersDriverDelayTask);
            CbdResellersDriver.TasksList.Add(CbdResellersDriverCjInvokeMemberGetFieldDataTask);

            CbdResellers.DriverStrategy = CbdResellersDriver;
            #endregion
            #region TransendLabs
            WebScrapperBaseSiteEntity TransendLabs = new WebScrapperBaseSiteEntity
            {
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                ItemUrl = "https://transcendlabs.com/sitemap_products_1.xml?from=3618014429268&to=4516823433300",
                BaseSiteUrl = "https://transcendlabs.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 5,
                SiteBaseRequestsPerSecondMax = 8,
                SiteBaseRequestsIntervalMin = 2,
                SiteBaseRequestsIntervalMax = 6
            };

            StrategyHtmlEntity TransendLabsNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h1.product-single__title",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity TransendLabsDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "div.product-single__description",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity TransendLabsPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "span.price-item.price-item--sale",
                AssignEntityTo = "productPrice"
            };

            StrategyHtmlEntity TransendLabsOldPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "span.price-item.price-item--regular",
                AssignEntityTo = "productSalePrice"
            };

            StrategyHtmlEntity TransendLabsImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".product-single__photo.js-zoom-enabled.product-single__photo--has-thumbnails.hide",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute TransendLabsImageSelectorSrc = new BaseItemStrategyAttribute
            {
                AttributeName = "data-zoom",
                AttributeAssingToRule = "imageUrl"
            };

            TransendLabsImageSelector.AttributesList.Add(TransendLabsImageSelectorSrc);

            StrategyHtmlEntity TransendLabsShopifyMetaSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyShopifyMetaScriptSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/script[15]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute TransendLabsShopifyMetaSelectorSku = new BaseItemStrategyAttribute
            {
                AttributeName = "sku",
                AttributeAssingToRule = "productCode"
            };

            BaseItemStrategyAttribute TransendLabsShopifyMetaSelectorVendor = new BaseItemStrategyAttribute
            {
                AttributeName = "vendor",
                AttributeAssingToRule = "productVendor"
            };

            TransendLabsShopifyMetaSelector.AttributesList.Add(TransendLabsShopifyMetaSelectorSku);
            TransendLabsShopifyMetaSelector.AttributesList.Add(TransendLabsShopifyMetaSelectorVendor);

            StrategyHtmlEntity TransendLabsInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                AssignEntityTo = "isProductInStock",
                BaseItemSelector = ".btn.product-form__cart-submit.btn--secondary-accent span",
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "Add to cart",
                    ResultIfPassed = true,
                    ResultIfFailed = false
                }
            };

            TransendLabs.ScrappingElements.Add(TransendLabsNameSelector);
            TransendLabs.ScrappingElements.Add(TransendLabsDescriptionSelector);
            TransendLabs.ScrappingElements.Add(TransendLabsPriceSelector);
            TransendLabs.ScrappingElements.Add(TransendLabsOldPriceSelector);
            TransendLabs.ScrappingElements.Add(TransendLabsImageSelector);
            TransendLabs.ScrappingElements.Add(TransendLabsShopifyMetaSelector);
            TransendLabs.ScrappingElements.Add(TransendLabsInStockSelector);

            var TransendLabsDriverStrategy = new BaseWebDriverStrategy();
            TransendLabsDriverStrategy.RequestUrl = "https://account.shareasale.com/a-customproductlink.cfm";
            TransendLabsDriverStrategy.TasksList = new List<BaseWebDriverTaskStrategy>();

            var TransendLabsDriverAuthUserNameStrategy = new BaseWebDriverTaskStrategy();
            TransendLabsDriverAuthUserNameStrategy.TaskType = BaseWebDriverTasksTypes.TaskUpdateFieldData;
            TransendLabsDriverAuthUserNameStrategy.RequestElement = "#username";
            TransendLabsDriverAuthUserNameStrategy.NewValue = "dev-artjoker";

            var TransendLabsDriverAuthUserPasswordStrategy = new BaseWebDriverTaskStrategy();
            TransendLabsDriverAuthUserPasswordStrategy.TaskType = BaseWebDriverTasksTypes.TaskUpdateFieldData;
            TransendLabsDriverAuthUserPasswordStrategy.RequestElement = "#password";
            TransendLabsDriverAuthUserPasswordStrategy.NewValue = "ef(*K:P=6%`xFb*=";

            var TransendLabsDriverAuthUserConfirmingStrategy = new BaseWebDriverTaskStrategy();
            TransendLabsDriverAuthUserConfirmingStrategy.TaskType = BaseWebDriverTasksTypes.TaskExecuteScript;
            TransendLabsDriverAuthUserConfirmingStrategy.ScriptSource = "document.getElementById('form1').submit()";

            var TransendLabsDriverAuthUserConfirmingDelayStrategy = new BaseWebDriverTaskStrategy();
            TransendLabsDriverAuthUserConfirmingDelayStrategy.TaskType = BaseWebDriverTasksTypes.TaskDelayTask;
            TransendLabsDriverAuthUserConfirmingDelayStrategy.DelayTime = 10000;

            var TransendLabsDriverSetUrlStrategy = new BaseWebDriverTaskStrategy();
            TransendLabsDriverSetUrlStrategy.TaskType = BaseWebDriverTasksTypes.TaskUpdateFieldData;
            TransendLabsDriverSetUrlStrategy.RequestElement = "#destinationURL";
            TransendLabsDriverSetUrlStrategy.ObtainFromField = "productUrl";

            var TransendLabsDriverUrlConfirmingStrategy = new BaseWebDriverTaskStrategy();
            TransendLabsDriverUrlConfirmingStrategy.TaskType = BaseWebDriverTasksTypes.TaskExecuteScript;
            TransendLabsDriverUrlConfirmingStrategy.ScriptSource = "document.getElementById('buildLinkFrm').children[7].children[0].click()";

            var TransendLabsDriverUrlNewUrlReceivingStrategy = new BaseWebDriverTaskStrategy();
            TransendLabsDriverUrlNewUrlReceivingStrategy.ScriptSource = "return document.getElementById(\"buildLinkFrm\").children[7].children[0].getAttribute(\"value\")";
            TransendLabsDriverUrlNewUrlReceivingStrategy.AssignToField = "productUrl";
            TransendLabsDriverUrlNewUrlReceivingStrategy.TaskType = BaseWebDriverTasksTypes.TaskGetDataFromPage;

            TransendLabsDriverStrategy.TasksList.Add(TransendLabsDriverAuthUserNameStrategy);
            TransendLabsDriverStrategy.TasksList.Add(TransendLabsDriverAuthUserPasswordStrategy);
            TransendLabsDriverStrategy.TasksList.Add(TransendLabsDriverAuthUserConfirmingStrategy);
            TransendLabsDriverStrategy.TasksList.Add(TransendLabsDriverAuthUserConfirmingDelayStrategy);
            TransendLabsDriverStrategy.TasksList.Add(TransendLabsDriverSetUrlStrategy);
            TransendLabsDriverStrategy.TasksList.Add(TransendLabsDriverUrlConfirmingStrategy);
            TransendLabsDriverAuthUserConfirmingDelayStrategy.DelayTime = 15000;
            TransendLabsDriverStrategy.TasksList.Add(TransendLabsDriverAuthUserConfirmingDelayStrategy);
            TransendLabsDriverStrategy.TasksList.Add(TransendLabsDriverUrlNewUrlReceivingStrategy);

            TransendLabs.DriverStrategy = TransendLabsDriverStrategy;

            #endregion
            #region PhilterLabs

            WebScrapperBaseSiteEntity PhilterLabs = new WebScrapperBaseSiteEntity
            {
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                ItemUrl = "https://philterlabs.com/sitemap_products_1.xml?from=1953642578033&to=4375851565169",
                BaseSiteUrl = "https://philterlabs.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 5,
                SiteBaseRequestsPerSecondMax = 8,
                SiteBaseRequestsIntervalMin = 2,
                SiteBaseRequestsIntervalMax = 6
            };

            StrategyHtmlEntity PhilterLabsNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h1.product_name",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity PhilterLabsDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "div.description.bottom",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity PhilterLabsPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".current_price .money",
                AssignEntityTo = "productPrice"
            };

            StrategyHtmlEntity PhilterLabsImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".lazyload.blur-up",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute PhilterLabsImageSelectorHref = new BaseItemStrategyAttribute
            {
                AttributeName = "data-src",
                AttributeAssingToRule = "imageUrl"
            };

            PhilterLabsImageSelector.AttributesList.Add(PhilterLabsImageSelectorHref);

            StrategyHtmlEntity PhilterLabsShopifyMetaScriptSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyShopifyMetaScriptSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/script[14]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute PhilterLabsShopifyMetaScriptSelectorSku = new BaseItemStrategyAttribute
            {
                AttributeName = "sku",
                AttributeAssingToRule = "productCode"
            };

            BaseItemStrategyAttribute PhilterLabsShopifyMetaScriptSelectorVendor = new BaseItemStrategyAttribute
            {
                AttributeName = "vendor",
                AttributeAssingToRule = "productVendor"
            };

            BaseItemStrategyAttribute PhilterLabsShopifyMetaScriptSelectorCategory = new BaseItemStrategyAttribute
            {
                AttributeName = "firstCategory",
                AttributeAssingToRule = "productCategory"
            };

            PhilterLabsShopifyMetaScriptSelector.AttributesList.Add(PhilterLabsShopifyMetaScriptSelectorSku);
            PhilterLabsShopifyMetaScriptSelector.AttributesList.Add(PhilterLabsShopifyMetaScriptSelectorVendor);
            PhilterLabsShopifyMetaScriptSelector.AttributesList.Add(PhilterLabsShopifyMetaScriptSelectorCategory);

            StrategyHtmlEntity PhilterLabsInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                AssignEntityTo = "isProductInStock",
                BaseItemSelector = ".add_to_cart .text",
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "Add to Cart",
                    ResultIfPassed = true,
                    ResultIfFailed = false
                }
            };

            PhilterLabs.ScrappingElements.Add(PhilterLabsNameSelector);
            PhilterLabs.ScrappingElements.Add(PhilterLabsDescriptionSelector);
            PhilterLabs.ScrappingElements.Add(PhilterLabsPriceSelector);
            PhilterLabs.ScrappingElements.Add(PhilterLabsImageSelector);
            PhilterLabs.ScrappingElements.Add(PhilterLabsShopifyMetaScriptSelector);
            PhilterLabs.ScrappingElements.Add(PhilterLabsInStockSelector);

            PhilterLabs.DriverStrategy = ShareAsaleDriverStrategy;

            #endregion
            #region SolCbd
            WebScrapperBaseSiteEntity SolCbd = new WebScrapperBaseSiteEntity
            {
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                ItemUrl = "https://www.solcbd.com/sitemap_products_1.xml?from=2324428741&to=4702461558872",
                BaseSiteUrl = "https://www.solcbd.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 5,
                SiteBaseRequestsPerSecondMax = 8,
                SiteBaseRequestsIntervalMin = 2,
                SiteBaseRequestsIntervalMax = 6
            };
            StrategyHtmlEntity SolCbdProductNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h1.product_name",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity SolCbdDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "div.description",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity SolCbdPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".current_price .money",
                AssignEntityTo = "productPrice"
            };

            StrategyHtmlEntity SolCbdOldPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".was_price .money",
                AssignEntityTo = "productSalePrice"
            };

            StrategyHtmlEntity SolCbdImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".lazyload.none",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute SolCbdImageSelectorLink = new BaseItemStrategyAttribute
            {
                AttributeName = "data-src",
                AttributeAssingToRule = "imageUrl"
            };

            SolCbdImageSelector.AttributesList.Add(SolCbdImageSelectorLink);

            StrategyHtmlEntity SolCbdShopifyMetaScriptSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyShopifyMetaScriptSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/script[14]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute SolCbdShopifyMetaScriptSelectorSku = new BaseItemStrategyAttribute
            {
                AttributeName = "sku",
                AttributeAssingToRule = "productCode"
            };

            BaseItemStrategyAttribute SolCbdShopifyMetaScriptSelectorVendor = new BaseItemStrategyAttribute
            {
                AttributeName = "vendor",
                AttributeAssingToRule = "productVendor"
            };

            BaseItemStrategyAttribute SolCbdShopifyMetaScriptSelectorType = new BaseItemStrategyAttribute
            {
                AttributeName = "type",
                AttributeAssingToRule = "productCategory"
            };

            SolCbdShopifyMetaScriptSelector.AttributesList.Add(SolCbdShopifyMetaScriptSelectorSku);
            SolCbdShopifyMetaScriptSelector.AttributesList.Add(SolCbdShopifyMetaScriptSelectorVendor);
            SolCbdShopifyMetaScriptSelector.AttributesList.Add(SolCbdShopifyMetaScriptSelectorType);

            StrategyHtmlEntity SolCbdInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                AssignEntityTo = "isProductInStock",
                BaseItemSelector = ".add_to_cart .text",
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "Add to Cart",
                    ResultIfPassed = true,
                    ResultIfFailed = false
                }
            };

            SolCbd.ScrappingElements.Add(SolCbdProductNameSelector);
            SolCbd.ScrappingElements.Add(SolCbdDescriptionSelector);
            SolCbd.ScrappingElements.Add(SolCbdPriceSelector);
            SolCbd.ScrappingElements.Add(SolCbdOldPriceSelector);
            SolCbd.ScrappingElements.Add(SolCbdImageSelector);
            SolCbd.ScrappingElements.Add(SolCbdInStockSelector);
            SolCbd.ScrappingElements.Add(SolCbdShopifyMetaScriptSelector);

            SolCbd.DriverStrategy = ShareAsaleDriverStrategy;
            #endregion
            #region SmellVeil
            WebScrapperBaseSiteEntity SmellVeil = new WebScrapperBaseSiteEntity
            {
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                ItemUrl = "https://smellveil.com/sitemap_products_1.xml?from=3556459020374&to=4357686394966",
                BaseSiteUrl = "https://smellveil.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 5,
                SiteBaseRequestsPerSecondMax = 8,
                SiteBaseRequestsIntervalMin = 2,
                SiteBaseRequestsIntervalMax = 6,
                ExternalHash = "/tbc"
            };

            StrategyHtmlEntity SmellVeilNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/title",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity SmellVeilDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "div.product_description",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity SmellVeilPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".product_price",
                AssignEntityTo = "productPrice"
            };

            StrategyHtmlEntity SmellVeilImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".product_image--container img",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute SmellVeilImageSelectorLink = new BaseItemStrategyAttribute
            {
                AttributeName = "src",
                AttributeAssingToRule = "imageUrl"
            };

            SmellVeilImageSelector.AttributesList.Add(SmellVeilImageSelectorLink);

            StrategyHtmlEntity SmellVeilShopifyScriptSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyShopifyMetaScriptSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/script[13]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute SmellVeilShopifyScriptSelectorSku = new BaseItemStrategyAttribute
            {
                AttributeName = "sku",
                AttributeAssingToRule = "productCode"
            };

            BaseItemStrategyAttribute SmellVeilShopifyScriptSelectorVendor = new BaseItemStrategyAttribute
            {
                AttributeName = "vendor",
                AttributeAssingToRule = "productVendor"
            };

            BaseItemStrategyAttribute SmellVeilShopifyScriptSelectorType = new BaseItemStrategyAttribute
            {
                AttributeName = "type",
                AttributeAssingToRule = "productCategory"
            };

            SmellVeilShopifyScriptSelector.AttributesList.Add(SmellVeilShopifyScriptSelectorSku);
            SmellVeilShopifyScriptSelector.AttributesList.Add(SmellVeilShopifyScriptSelectorVendor);
            SmellVeilShopifyScriptSelector.AttributesList.Add(SmellVeilShopifyScriptSelectorType);

            StrategyHtmlEntity SmellVeilInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".action_button",
                AssignEntityTo = "isProductInStock",
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "Add to Cart",
                    ResultIfPassed = true,
                    ResultIfFailed = false
                }
            };

            SmellVeil.ScrappingElements.Add(SmellVeilNameSelector);
            SmellVeil.ScrappingElements.Add(SmellVeilDescriptionSelector);
            SmellVeil.ScrappingElements.Add(SmellVeilPriceSelector);
            SmellVeil.ScrappingElements.Add(SmellVeilImageSelector);
            SmellVeil.ScrappingElements.Add(SmellVeilShopifyScriptSelector);
            SmellVeil.ScrappingElements.Add(SmellVeilInStockSelector);


            #endregion
            #region SlickVapes
            WebScrapperBaseSiteEntity SlickVapes = new WebScrapperBaseSiteEntity
            {
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                ExternalHash = "?aff=134",
                ItemUrl = "https://slickvapes.com/sitemap_products_1.xml?from=5863235457&to=4698849181790",
                BaseSiteUrl = "https://slickvapes.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 5,
                SiteBaseRequestsPerSecondMax = 8,
                SiteBaseRequestsIntervalMin = 2,
                SiteBaseRequestsIntervalMax = 6
            };

            StrategyHtmlEntity SlickVapesNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".grid__item.large--one-half h1",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity SlickVapesDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "div.product-description",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity SlickVapesPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "span#ProductPrice",
                AssignEntityTo = "productPrice"
            };

            StrategyHtmlEntity SlickVapesOldPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "#ComparePrice",
                AssignEntityTo = "productSalePrice"
            };

            StrategyHtmlEntity SlickVapesImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategyMultipleSelection,
                BaseItemSelector = "#slider .slides li",
                AssignEntityTo = "imageurl",
                SelectByIndexFromRange = "7",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute SlickVapesImageSelectorLink = new BaseItemStrategyAttribute
            {
                AttributeName = "src",
                AttributeAssingToRule = "imageUrl"
            };

            SlickVapesImageSelector.AttributesList.Add(SlickVapesImageSelectorLink);

            StrategyHtmlEntity SlickVapesShopifyMetaScriptSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyShopifyMetaScriptSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/script[11]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute SlickVapesShopifyMetaScriptSelectorSku = new BaseItemStrategyAttribute
            {
                AttributeName = "sku",
                AttributeAssingToRule = "productCode"
            };

            BaseItemStrategyAttribute SlickVapesShopifyMetaScriptSelectorVendor = new BaseItemStrategyAttribute
            {
                AttributeName = "vendor",
                AttributeAssingToRule = "productVendor"
            };

            SlickVapesShopifyMetaScriptSelector.AttributesList.Add(SlickVapesShopifyMetaScriptSelectorSku);
            SlickVapesShopifyMetaScriptSelector.AttributesList.Add(SlickVapesShopifyMetaScriptSelectorVendor);

            SlickVapes.ScrappingElements.Add(SlickVapesNameSelector);
            SlickVapes.ScrappingElements.Add(SlickVapesDescriptionSelector);
            SlickVapes.ScrappingElements.Add(SlickVapesPriceSelector);
            SlickVapes.ScrappingElements.Add(SlickVapesOldPriceSelector);
            SlickVapes.ScrappingElements.Add(SlickVapesImageSelector);
            SlickVapes.ScrappingElements.Add(SlickVapesShopifyMetaScriptSelector);
            #endregion
            #region BuyCbdCigarettes

            WebScrapperBaseSiteEntity BuyCbdCigarettes = new WebScrapperBaseSiteEntity
            {
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify, // If fact WP, but it has the samest sitemap
                ItemUrl = "https://buycbdcigarettes.com/product-sitemap.xml",
                BaseSiteUrl = "https://buycbdcigarettes.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 5,
                SiteBaseRequestsPerSecondMax = 8,
                SiteBaseRequestsIntervalMin = 2,
                SiteBaseRequestsIntervalMax = 6,
                ExcludeUrlsByParts = new List<string>()
            };

            BuyCbdCigarettes.ExcludeUrlsByParts.Add("/shop/");

            StrategyHtmlEntity BuyCbdCigarettesNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "div.product-title product_title entry-title",
                AssignEntityTo = "h1.product-title"
            };

            StrategyHtmlEntity BuyCbdCigarettesDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "#tab-description",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity BuyCbdCigarettesPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".price.product-page-price .woocommerce-Price-amount.amount",
                AssignEntityTo = "productPrice"
            };

            StrategyHtmlEntity BuyCbdCigarettesOldPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "",
                AssignEntityTo = "productSalePrice"
            };

            StrategyHtmlEntity BuyCbdCigarettesImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/meta[12]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute BuyCbdCigarettesImageSelectorLink = new BaseItemStrategyAttribute
            {
                AttributeName = "content",
                AttributeAssingToRule = "imageUrl"
            };

            BuyCbdCigarettesImageSelector.AttributesList.Add(BuyCbdCigarettesImageSelectorLink);

            StrategyHtmlEntity BuyCbdCigarettesCategorySelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategyMultipleSelection,
                BaseItemSelector = ".woocommerce-breadcrumb.breadcrumbs.uppercase a",
                AssignEntityTo = "productCategory",
                SelectByIndexFromRange = "last"
            };

            StrategyHtmlEntity BuyCbdCigarettesSkuSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".sku_wrapper .sku",
                AssignEntityTo = "productCode"
            };

            StrategyHtmlEntity BuyCbdCigarettesInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".stock.out-of-stock",
                AssignEntityTo = "isProductInStock",
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Null",
                    ResultIfPassed = false,
                    ResultIfFailed = true
                }
            };

            StrategyHtmlEntity BuyCbdCigarettesVendorSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h1.product-title",
                AssignEntityTo = "productVendor"
            };

            BuyCbdCigarettes.ScrappingElements.Add(BuyCbdCigarettesNameSelector);
            BuyCbdCigarettes.ScrappingElements.Add(BuyCbdCigarettesDescriptionSelector);
            BuyCbdCigarettes.ScrappingElements.Add(BuyCbdCigarettesPriceSelector);
            BuyCbdCigarettes.ScrappingElements.Add(BuyCbdCigarettesOldPriceSelector);
            BuyCbdCigarettes.ScrappingElements.Add(BuyCbdCigarettesImageSelector);
            BuyCbdCigarettes.ScrappingElements.Add(BuyCbdCigarettesCategorySelector);
            BuyCbdCigarettes.ScrappingElements.Add(BuyCbdCigarettesInStockSelector);
            BuyCbdCigarettes.ScrappingElements.Add(BuyCbdCigarettesVendorSelector);

            BaseWebDriverStrategy BuyCbdCigarettesDriverChrome = new BaseWebDriverStrategy
            {
                RequestUrl = "--current--",
                TasksList = new List<BaseWebDriverTaskStrategy>()
            };

            BaseWebDriverTaskStrategy BuyCbdCigarettesDriverChromePriceStrategy = new BaseWebDriverTaskStrategy
            {
                TaskType = BaseWebDriverTasksTypes.TaskGetDataFromPage,
                ScriptSource = "return TaskGetCbdCigarettesPrice();function TaskGetCbdCigarettesPrice(){$=jQuery;var e=$(\".price.product-page-price\");if(e.length>0)return TaskGetLowestPrice(e)}function TaskGetLowestPrice(e){return TaskHasSalePrice(e)?$(e).find(\"ins .woocommerce-Price-amount.amount\").text():TaskIsRange(e)?$($(e).find(\".woocommerce-Price-amount\")[0]).text():$(e).find(\".woocommerce-Price-amount.amount\").text()}function TaskHasSalePrice(e){return!!e.hasClass(\"price-on-sale\")}function TaskIsRange(e){return $(e).text().indexOf(\" – \")>-1}",
                AssignToField = "productPrice"
            };

            BuyCbdCigarettesDriverChrome.TasksList.Add(BuyCbdCigarettesDriverChromePriceStrategy);

            BuyCbdCigarettes.DriverStrategy = BuyCbdCigarettesDriverChrome;

            #endregion
            #region BrothersWithGlass

            WebScrapperBaseSiteEntity BrothersWithGlass = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://brotherswithglass.com/sitemap_products_1.xml?from=6753098177&to=5568862421143",
                BaseSiteUrl = "https://brotherswithglass.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ExternalHash = "?rfsn=2993258.a1d06e",
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 5,
                SiteBaseRequestsPerSecondMax = 8,
                SiteBaseRequestsIntervalMin = 2,
                SiteBaseRequestsIntervalMax = 6,
                CollectionsProcessor = new WebScrapperBaseCollectionsProcessorEntity
                {
                    IsCategoriesPreparingRequired = true,
                    ProcessingType = WebScrapperBaseCollectionsProcessorEntityProcessingType.ProcessByDriver,
                    DriverSettings = new BaseWebDriverStrategy
                    {
                        RequestUrl = "https://brotherswithglass.com/",
                        TasksList = new List<BaseWebDriverTaskStrategy>()
                    },
                    Collections = new List<string>()
                }
            };

            BaseWebDriverTaskStrategy BrothersWithGlassCollectionsTaskDriver = new BaseWebDriverTaskStrategy
            {
                TaskType = BaseWebDriverTasksTypes.TaskExecuteScript,
                ScriptSource = "scripts/js/brotherwithGlass/TaskGetCollections.js",
                ScriptSourceType = WebScrapper.Scrapper.Entities.enums.By.FileSource
            };

            BrothersWithGlass.CollectionsProcessor.DriverSettings.TasksList.Add(BrothersWithGlassCollectionsTaskDriver);

            StrategyHtmlEntity BrothersWithGlassProductNameSelecor = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h1.product-meta__title",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity BrothersWithGlassProductImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/meta[8]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute BrothersWithGlassProductImageLink = new BaseItemStrategyAttribute
            {
                AttributeName = "content",
                AttributeAssingToRule = "imageUrl"
            };

            BrothersWithGlassProductImageSelector.AttributesList.Add(BrothersWithGlassProductImageLink);

            StrategyHtmlEntity BrothersWithGlassProductDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".rte.text--pull",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity BrothersWithGlassProductPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".price",
                AssignEntityTo = "productPrice"
            };

            StrategyHtmlEntity BrothersWithGlassProductOldPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".price.price--compare",
                AssignEntityTo = "productSalePrice"
            };

            StrategyHtmlEntity BrothersWithGlassProductVendorSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".product-meta__vendor",
                AssignEntityTo = "productVendor"
            };

            StrategyHtmlEntity BrothersWithGlassProductSkuSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".product-meta__sku-number",
                AssignEntityTo = "productCode"
            };

            StrategyHtmlEntity BrothersWithGlassShopifyMetaCodeSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyShopifyMetaScriptSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/script[13]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute BrothersWithGlassShopifyMetaCodeTypeAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "type",
                AttributeAssingToRule = "productCategory"
            };

            BrothersWithGlassShopifyMetaCodeSelector.AttributesList.Add(BrothersWithGlassShopifyMetaCodeTypeAttribute);

            StrategyHtmlEntity BrothersWithGlassInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".product-form__inventory",
                AssignEntityTo = "isProductInStock",
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "In stock",
                    ResultIfPassed = true,
                    ResultIfFailed = false
                }
            };

            StrategyHtmlEntity BrothersWithGlassTagsSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyShopifyProductJsonTemplate,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/body/main/div[1]/section/div[1]/div[2]/div/div[2]/div/script",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute BrothersWithGlassTagsAttributeTags = new BaseItemStrategyAttribute
            {
                AttributeName = "ProductLimitedTags",
                AttributeAssingToRule = "productTags"
            };

            BrothersWithGlassTagsSelector.AttributesList.Add(BrothersWithGlassTagsAttributeTags);

            BrothersWithGlass.ScrappingElements.Add(BrothersWithGlassProductNameSelecor);
            BrothersWithGlass.ScrappingElements.Add(BrothersWithGlassProductDescriptionSelector);
            BrothersWithGlass.ScrappingElements.Add(BrothersWithGlassProductPriceSelector);
            BrothersWithGlass.ScrappingElements.Add(BrothersWithGlassProductOldPriceSelector);
            BrothersWithGlass.ScrappingElements.Add(BrothersWithGlassProductVendorSelector);
            BrothersWithGlass.ScrappingElements.Add(BrothersWithGlassProductSkuSelector);
            BrothersWithGlass.ScrappingElements.Add(BrothersWithGlassShopifyMetaCodeSelector);
            BrothersWithGlass.ScrappingElements.Add(BrothersWithGlassInStockSelector);
            BrothersWithGlass.ScrappingElements.Add(BrothersWithGlassTagsSelector);
            BrothersWithGlass.ScrappingElements.Add(BrothersWithGlassProductImageSelector);

            #endregion      
            #region CbdNaturals

            WebScrapperBaseSiteEntity CbdNaturals = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://www.cbdnaturals.com/",
                BaseSiteUrl = "https://www.cbdnaturals.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ExternalHash = "?affid=865769d3-71f7-46d4-b33b-0857a26a65ea",
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsAnother,
                SiteProductPageIndicationSelector = "h1.product-title",
                ScrappingElements = new List<StrategyHtmlEntity>(),
                ExcludeUrlsByParts = new List<string>(),
                SiteBaseRequestsPerSecondMin = 5,
                SiteBaseRequestsPerSecondMax = 8,
                SiteBaseRequestsIntervalMin = 2,
                SiteBaseRequestsIntervalMax = 6
            };

            StrategyHtmlEntity CbdNaturalsProductNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h1.product-title",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity CbdNaturalsProductImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/meta[8]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute CbdNaturalsProductImageLinkAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "content",
                AttributeAssingToRule = "imageUrl"
            };

            CbdNaturalsProductImageSelector.AttributesList.Add(CbdNaturalsProductImageLinkAttribute);

            StrategyHtmlEntity CbdNaturalsDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/body/div[4]/div[1]/div[2]/div[2]",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity CbdNaturalsPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".text-right b",
                AssignEntityTo = "productPrice"
            };

            StrategyHtmlEntity CbdNaturalsOldPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "del.text-danger",
                AssignEntityTo = "productSalePrice"
            };

            StrategyHtmlEntity CbdNaturalsSchemaSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategySchemaOrgSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "script",
                SelectByIndexFromRange = "12",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute CbdNaturalsSchemaSkuAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "SchemaSku",
                AttributeAssingToRule = "productCode"
            };

            BaseItemStrategyAttribute CbdNaturalsSchemaBrandAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "ProductBrand",
                AttributeAssingToRule = "productVendor"
            };

            CbdNaturalsSchemaSelector.AttributesList.Add(CbdNaturalsSchemaSkuAttribute);
            CbdNaturalsSchemaSelector.AttributesList.Add(CbdNaturalsSchemaBrandAttribute);

            StrategyHtmlEntity CbdNaturalsInstockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".btn.btn-block.btn-info",
                AssignEntityTo = "isProductInStock",
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "Add To Cart",
                    ResultIfPassed = true,
                    ResultIfFailed = false
                }
            };

            CbdNaturals.ScrappingElements.Add(CbdNaturalsProductNameSelector);
            CbdNaturals.ScrappingElements.Add(CbdNaturalsProductImageSelector);
            CbdNaturals.ScrappingElements.Add(CbdNaturalsDescriptionSelector);
            CbdNaturals.ScrappingElements.Add(CbdNaturalsPriceSelector);
            CbdNaturals.ScrappingElements.Add(CbdNaturalsOldPriceSelector);
            CbdNaturals.ScrappingElements.Add(CbdNaturalsSchemaSelector);
            CbdNaturals.ScrappingElements.Add(CbdNaturalsInstockSelector);

            CbdNaturals.ExcludeUrlsByParts.Add("/blogs");
            CbdNaturals.ExcludeUrlsByParts.Add("/blog");
            CbdNaturals.ExcludeUrlsByParts.Add("/about");
            CbdNaturals.ExcludeUrlsByParts.Add("/founder");
            CbdNaturals.ExcludeUrlsByParts.Add("/core-values");
            CbdNaturals.ExcludeUrlsByParts.Add("/partners/");
            CbdNaturals.ExcludeUrlsByParts.Add("/cart");
            CbdNaturals.ExcludeUrlsByParts.Add("/account");
            CbdNaturals.ExcludeUrlsByParts.Add("/brands");
            CbdNaturals.ExcludeUrlsByParts.Add("/review");

            #endregion
            #region CbdPure

            WebScrapperBaseSiteEntity CbdPure = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://www.cbdpure.com/",
                BaseSiteUrl = "https://www.cbdpure.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ExternalHash = "?AFFID=406664",
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsAnother,
                SiteProductPageIndicationSelector = "/html/body/div[1]/div[2]/div[2]/h1",
                ScrappingElements = new List<StrategyHtmlEntity>(),
                ExcludeUrlsByParts = new List<string>(),
                SiteBaseRequestsPerSecondMin = 5,
                SiteBaseRequestsPerSecondMax = 8,
                SiteBaseRequestsIntervalMin = 2,
                SiteBaseRequestsIntervalMax = 6
            };

            StrategyHtmlEntity CbdPureProductNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/body/div[1]/div[2]/div[2]/h1",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity CbdPureProductImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "#image-gallery li div a img",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute CbdPureProductImageLinkAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "src",
                AttributeAssingToRule = "imageUrl"
            };

            CbdPureProductImageSelector.AttributesList.Add(CbdPureProductImageLinkAttribute);

            StrategyHtmlEntity CbdPureProductSchemaEntitySelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategySchemaOrgSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "script",
                SelectByIndexFromRange = "1",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute CbdPureProductSchemaEntityCode = new BaseItemStrategyAttribute
            {
                AttributeName = "SchemaSku",
                AttributeAssingToRule = "productCode"
            };

            BaseItemStrategyAttribute CbdPureProductSchemaEntityImage = new BaseItemStrategyAttribute
            {
                AttributeName = "SchemaImage",
                AttributeAssingToRule = "imageUrl"
            };

            BaseItemStrategyAttribute CbdPureProductSchemaEntityBrand = new BaseItemStrategyAttribute
            {
                AttributeName = "ProductBrand",
                AttributeAssingToRule = "productVendor"
            };

            BaseItemStrategyAttribute CbdPureProductSchemaEntityDescription = new BaseItemStrategyAttribute
            {
                AttributeName = "SchemaDescription",
                AttributeAssingToRule = "productDescription"
            };

            BaseItemStrategyAttribute CbdPureProductSchemaEntityPrice = new BaseItemStrategyAttribute
            {
                AttributeName = "Price",
                AttributeAssingToRule = "productPrice"
            };

            CbdPureProductSchemaEntitySelector.AttributesList.Add(CbdPureProductSchemaEntityCode);
            CbdPureProductSchemaEntitySelector.AttributesList.Add(CbdPureProductSchemaEntityImage);
            CbdPureProductSchemaEntitySelector.AttributesList.Add(CbdPureProductSchemaEntityBrand);
            CbdPureProductSchemaEntitySelector.AttributesList.Add(CbdPureProductSchemaEntityDescription);
            CbdPureProductSchemaEntitySelector.AttributesList.Add(CbdPureProductSchemaEntityPrice);

            StrategyHtmlEntity CbdPureInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/body/div[1]/div[2]/div[2]/button",
                AssignEntityTo = "isProductInStock",
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "ADD TO CART",
                    ResultIfPassed = true,
                    ResultIfFailed = false
                }
            };

            CbdPure.ScrappingElements.Add(CbdPureProductNameSelector);
            CbdPure.ScrappingElements.Add(CbdPureProductImageSelector);
            CbdPure.ScrappingElements.Add(CbdPureProductSchemaEntitySelector);
            CbdPure.ScrappingElements.Add(CbdPureInStockSelector);

            CbdPure.ExcludeUrlsByParts.Add("what-is-cbdpure.html");
            CbdPure.ExcludeUrlsByParts.Add("faq.html");
            CbdPure.ExcludeUrlsByParts.Add("guarantee.html");
            CbdPure.ExcludeUrlsByParts.Add("contact.html");
            CbdPure.ExcludeUrlsByParts.Add("order.html");
            CbdPure.ExcludeUrlsByParts.Add("#");
            CbdPure.ExcludeUrlsByParts.Add("/blog/");
            CbdPure.ExcludeUrlsByParts.Add("lab-test-results");
            CbdPure.ExcludeUrlsByParts.Add("order-mailfax.html");
            CbdPure.ExcludeUrlsByParts.Add("resources.html");
            CbdPure.ExcludeUrlsByParts.Add("about-us.html");
            CbdPure.ExcludeUrlsByParts.Add("privacy.html");
            CbdPure.ExcludeUrlsByParts.Add("terms.html");
            CbdPure.ExcludeUrlsByParts.Add("/affiliate/");
            CbdPure.ExcludeUrlsByParts.Add("wholesale.html");
            CbdPure.ExcludeUrlsByParts.Add("sitemap.html");
            CbdPure.ExcludeUrlsByParts.Add("organic-standards.html");
            CbdPure.ExcludeUrlsByParts.Add(".jpg");

            #endregion
            #region Cbdvapejuice

            WebScrapperBaseSiteEntity Cbdvapejuice = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://cbdvapejuice.net/",
                BaseSiteUrl = "https://cbdvapejuice.net/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ExternalHash = "ref/alexoutlaw1981/",
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsAnother,
                SiteProductPageIndicationSelector = "h1.product_title",
                ScrappingElements = new List<StrategyHtmlEntity>(),
                ExcludeUrlsByParts = new List<string>(),
                SiteBaseRequestsPerSecondMin = 5,
                SiteBaseRequestsPerSecondMax = 8,
                SiteBaseRequestsIntervalMin = 2,
                SiteBaseRequestsIntervalMax = 6,
                DefaultStockValue = true
            };

            StrategyHtmlEntity CbdvapejuiceProductNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h1.product_title",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity CbdvapejuiceProductImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/meta[12]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute CbdvapejuiceProductImageLinkAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "content",
                AttributeAssingToRule = "imageUrl"
            };

            CbdvapejuiceProductImageSelector.AttributesList.Add(CbdvapejuiceProductImageLinkAttribute);

            StrategyHtmlEntity CbdvapejuiceDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "#tab-description",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity CbdvapejuiceVendorSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "//*[@id=\"page\"]/div[2]/div/nav/a[3]",
                AssignEntityTo = "productVendor"
            };

            Cbdvapejuice.ScrappingElements.Add(CbdvapejuiceProductNameSelector);
            Cbdvapejuice.ScrappingElements.Add(CbdvapejuiceProductImageSelector);
            Cbdvapejuice.ScrappingElements.Add(CbdvapejuiceDescriptionSelector);
            Cbdvapejuice.ScrappingElements.Add(CbdvapejuiceVendorSelector);

            BaseWebDriverStrategy CbdvapejuiceDriver = new BaseWebDriverStrategy
            {
                RequestUrl = "--current--",
                TasksList = new List<BaseWebDriverTaskStrategy>()
            };

            BaseWebDriverTaskStrategy CbdvapejuiceDriverPriceSelector = new BaseWebDriverTaskStrategy
            {
                TaskType = BaseWebDriverTasksTypes.TaskGetDataFromPage,
                ScriptSourceType = WebScrapper.Scrapper.Entities.enums.By.FileSource,
                ScriptSource = "scripts/js/cbdvapejuice/TaskGetCbdVapeJuicePrice.js",
                AssignToField = "productPrice"
            };

            CbdvapejuiceDriver.TasksList.Add(CbdvapejuiceDriverPriceSelector);

            Cbdvapejuice.DriverStrategy = CbdvapejuiceDriver;

            #endregion
            #region Getmav

            WebScrapperBaseSiteEntity Getmax = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://www.getmav.com/sitemap_products_1.xml?from=3964882976862&to=4731877752926",
                BaseSiteUrl = "https://www.getmav.com/",
                ProductFamily = "SharpTest",
                ExternalHash = "?rfsn=4149927.3aad34&amp;utm_source=refersion&amp;utm_medium=affiliate&amp;utm_campaign=4149927.3aad34",
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 8,
                SiteBaseRequestsPerSecondMax = 15,
                SiteBaseRequestsIntervalMin = 10,
                SiteBaseRequestsIntervalMax = 20
            };

            StrategyHtmlEntity GetmaxProductNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h1.product_name",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity GetmaxProductImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "a.lightbox",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute GetmaxProductImageLinkAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "href",
                AttributeAssingToRule = "imageUrl"
            };

            GetmaxProductImageSelector.AttributesList.Add(GetmaxProductImageLinkAttribute);

            StrategyHtmlEntity GetmaxDescriptionSelecor = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "#tab1",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity GetmaxMetaSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyShopifyMetaScriptSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/script[12]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute GetmaxMetaVendorAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "vendor",
                AttributeAssingToRule = "productVendor"
            };

            BaseItemStrategyAttribute GetmaxMetaTypeAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "type",
                AttributeAssingToRule = "productCategory"
            };

            BaseItemStrategyAttribute GetmaxMetaSkuAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "sku",
                AttributeAssingToRule = "productCode"
            };

            GetmaxMetaSelector.AttributesList.Add(GetmaxMetaVendorAttribute);
            GetmaxMetaSelector.AttributesList.Add(GetmaxMetaTypeAttribute);
            GetmaxMetaSelector.AttributesList.Add(GetmaxMetaSkuAttribute);

            StrategyHtmlEntity GetmaxPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".current_price",
                AssignEntityTo = "productPrice"
            };

            StrategyHtmlEntity GetmaxOldPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "productSalePrice",
                AssignEntityTo = ".was_price"
            };

            StrategyHtmlEntity GetmaxInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                AssignEntityTo = "isProductInStock",
                BaseItemSelector = ".sold_out",
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "Unavailable",
                    ResultIfPassed = false,
                    ResultIfFailed = true
                }
            };

            Getmax.ScrappingElements.Add(GetmaxProductNameSelector);
            Getmax.ScrappingElements.Add(GetmaxProductImageSelector);
            Getmax.ScrappingElements.Add(GetmaxDescriptionSelecor);
            Getmax.ScrappingElements.Add(GetmaxMetaSelector);
            Getmax.ScrappingElements.Add(GetmaxPriceSelector);
            Getmax.ScrappingElements.Add(GetmaxOldPriceSelector);
            Getmax.ScrappingElements.Add(GetmaxInStockSelector);

            #endregion
            #region IsmokeFresh

            WebScrapperBaseSiteEntity Ismokefresh = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://ismokefresh.com/sitemap_products_1.xml?from=1603147169828&to=4869586190372",
                BaseSiteUrl = "https://ismokefresh.com/",
                ProductFamily = "SharpTest",
                ExternalHash = "?ref=ktdoft00a4y",
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 8,
                SiteBaseRequestsPerSecondMax = 10,
                SiteBaseRequestsIntervalMin = 10,
                SiteBaseRequestsIntervalMax = 20
            };

            StrategyHtmlEntity IsmokefreshProductNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h1.product_name",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity IsmokefreshProductImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "a.lightbox",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute IsmokefreshProductImageLinkAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "href",
                AttributeAssingToRule = "imageUrl"
            };

            IsmokefreshProductImageSelector.AttributesList.Add(IsmokefreshProductImageLinkAttribute);

            StrategyHtmlEntity IsmokefreshProductDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".description",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity IsmokefreshProductMetaSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyShopifyMetaScriptSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/script[10]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute IsmokefreshMetaVendorAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "vendor",
                AttributeAssingToRule = "productVendor"
            };

            BaseItemStrategyAttribute IsmokefreshMetaTypeAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "type",
                AttributeAssingToRule = "productCategory"
            };

            BaseItemStrategyAttribute IsmokefreshMetaSkuAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "sku",
                AttributeAssingToRule = "productCode"
            };
            IsmokefreshProductMetaSelector.AttributesList.Add(IsmokefreshMetaVendorAttribute);
            IsmokefreshProductMetaSelector.AttributesList.Add(IsmokefreshMetaTypeAttribute);
            IsmokefreshProductMetaSelector.AttributesList.Add(IsmokefreshMetaSkuAttribute);

            StrategyHtmlEntity IsmokefreshProductPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".current_price",
                AssignEntityTo = "productPrice"
            };

            StrategyHtmlEntity IsmokefreshProductOldPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".was_price",
                AssignEntityTo = "productSalePrice"
            };

            StrategyHtmlEntity IsmokefreshInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                AssignEntityTo = "isProductInStock",
                BaseItemSelector = ".sold_out",
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "Unavailable",
                    ResultIfPassed = false,
                    ResultIfFailed = true
                }
            };

            Ismokefresh.ScrappingElements.Add(IsmokefreshProductNameSelector);
            Ismokefresh.ScrappingElements.Add(IsmokefreshProductImageSelector);
            Ismokefresh.ScrappingElements.Add(IsmokefreshProductDescriptionSelector);
            Ismokefresh.ScrappingElements.Add(IsmokefreshProductMetaSelector);
            Ismokefresh.ScrappingElements.Add(IsmokefreshProductPriceSelector);
            Ismokefresh.ScrappingElements.Add(IsmokefreshProductOldPriceSelector);
            Ismokefresh.ScrappingElements.Add(IsmokefreshInStockSelector);

            BaseWebDriverStrategy IsmokefreshDriver = new BaseWebDriverStrategy
            {
                RequestUrl = "--current--",
                TasksList = new List<BaseWebDriverTaskStrategy>()
            };

            BaseWebDriverTaskStrategy IsmokefreshDriverCategoryTask = new BaseWebDriverTaskStrategy
            {
                TaskType = BaseWebDriverTasksTypes.TaskGetDataFromPage,
                ScriptSource = "return TaskGetCategory();function TaskGetCategory(){var t;return $(\".product_links\").find(\"p\").each(function(){\"Collections:\"==$(this).find(\"span.label\").text()&&$(this).find(\"a\").each(function(){return t=$(this).text(),!1})}),t}",
                ScriptSourceType = WebScrapper.Scrapper.Entities.enums.By.StringSource,
                AssignToField = "productCategory"
            };

            BaseWebDriverTaskStrategy IsmokefreshDriverTagsTask = new BaseWebDriverTaskStrategy
            {
                TaskType = BaseWebDriverTasksTypes.TaskGetDataFromPage,
                ScriptSource = "return TaskGetCategory();function TaskGetCategory(){var t=[];return $(\".product_links\").find(\"p\").each(function(){\"Category:\"==$(this).find(\"span.label\").text()&&$(this).find(\"a\").each(function(){t.push($(this).text())})}),t.join(\",\")}",
                ScriptSourceType = WebScrapper.Scrapper.Entities.enums.By.StringSource,
                AssignToField = "productTags"
            };

            IsmokefreshDriver.TasksList.Add(IsmokefreshDriverCategoryTask);
            IsmokefreshDriver.TasksList.Add(IsmokefreshDriverTagsTask);

            Ismokefresh.DriverStrategy = IsmokefreshDriver;
            #endregion
            #region Gotvape - Paused, issues detected

            WebScrapperBaseSiteEntity Gotvape = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://www.gotvape.com/",
                BaseSiteUrl = "https://www.gotvape.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ExternalHash = "?acc=aa942ab2bfa6ebda4840e7360ce6e7ef",
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsAnother,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                ExcludeUrlsByParts = new List<string>(),
                SiteBaseRequestsPerSecondMin = 1,
                SiteBaseRequestsPerSecondMax = 5,
                SiteBaseRequestsIntervalMin = 10,
                SiteBaseRequestsIntervalMax = 20,
                SiteProductPageIndicationSelector = "//*[@id=\"product_addtocart_form\"]/div[3]/h1"
            };

            StrategyHtmlEntity GotvapeProductNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "//*[@id=\"product_addtocart_form\"]/div[3]/h1",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity GotvapeProductImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "#gallery-image",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute GotvapeProductImageLinkAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "href",
                AttributeAssingToRule = "imageUrl"
            };

            GotvapeProductImageSelector.AttributesList.Add(GotvapeProductImageLinkAttribute);

            StrategyHtmlEntity GotvapeProductDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".product-description div",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity GotvapeProductVendorSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "",
                AssignEntityTo = "productVendor"
            };

            StrategyHtmlEntity GotvapeProductPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".regular-price .price",
                AssignEntityTo = "productPrice"
            };

            StrategyHtmlEntity GotvapeProductInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,

            };

            Gotvape.ScrappingElements.Add(GotvapeProductNameSelector);
            Gotvape.ScrappingElements.Add(GotvapeProductImageSelector);
            Gotvape.ScrappingElements.Add(GotvapeProductDescriptionSelector);
            Gotvape.ScrappingElements.Add(GotvapeProductPriceSelector);

            #endregion
            #region TankGlass

            WebScrapperBaseSiteEntity TankGlass = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://tankglass.com/sitemap_products_1.xml?from=1491075170359&to=4559840903286",
                BaseSiteUrl = "https://tankglass.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ExternalHash = "?rfsn=3760146.e1f913",
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                SiteBaseRequestsPerSecondMin = 8,
                SiteBaseRequestsPerSecondMax = 12,
                SiteBaseRequestsIntervalMin = 10,
                SiteBaseRequestsIntervalMax = 20
            };

            StrategyHtmlEntity TankGlassProductNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".product-desc h1.h2",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity TankGlassProductImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/meta[12]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute TankGlassProductImageLinkAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "content",
                AttributeAssingToRule = "imageUrl"
            };

            TankGlassProductImageSelector.AttributesList.Add(TankGlassProductImageLinkAttribute);

            StrategyHtmlEntity TankGlassProductDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".product-description.rte",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity TankGlassShopifyMetaSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyShopifyMetaScriptSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/script[17]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute TankGlassShopifyMetaVendorAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "vendor",
                AttributeAssingToRule = "productVendor"
            };

            BaseItemStrategyAttribute TankGlassShopifyMetaSkuAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "sku",
                AttributeAssingToRule = "productCode"
            };

            BaseItemStrategyAttribute TankGlassShopifyMetaTypeAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "type",
                AttributeAssingToRule = "productCategory"
            };

            TankGlassShopifyMetaSelector.AttributesList.Add(TankGlassShopifyMetaVendorAttribute);
            TankGlassShopifyMetaSelector.AttributesList.Add(TankGlassShopifyMetaSkuAttribute);
            TankGlassShopifyMetaSelector.AttributesList.Add(TankGlassShopifyMetaTypeAttribute);

            StrategyHtmlEntity TankGlassProductPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/meta[9]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute TankGlassProductPriceContentAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "content",
                AttributeAssingToRule = "productPrice"
            };

            TankGlassProductPriceSelector.AttributesList.Add(TankGlassProductPriceContentAttribute);

            StrategyHtmlEntity TankGlassProductOldPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".sale-tag.large",
                AssignEntityTo = "productSalePrice"
            };

            StrategyHtmlEntity TankGlassProductInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "#addToCart",
                AssignEntityTo = "isProductInStock",
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "Sold Out",
                    ResultIfFailed = true,
                    ResultIfPassed = false
                }
            };

            TankGlass.ScrappingElements.Add(TankGlassProductNameSelector);
            TankGlass.ScrappingElements.Add(TankGlassProductImageSelector);
            TankGlass.ScrappingElements.Add(TankGlassProductDescriptionSelector);
            TankGlass.ScrappingElements.Add(TankGlassShopifyMetaSelector);
            TankGlass.ScrappingElements.Add(TankGlassProductPriceSelector);
            TankGlass.ScrappingElements.Add(TankGlassProductOldPriceSelector);
            TankGlass.ScrappingElements.Add(TankGlassProductInStockSelector);

            #endregion
            #region GreenWaterPipes

            WebScrapperBaseSiteEntity GreenWaterPipes = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://greenwaterpipes.com/sitemap_products_1.xml?from=1760068829250&to=5334352724126",
                BaseSiteUrl = "https://greenwaterpipes.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ExternalHash = "?sca_ref=1066.xUn0K0mudk",
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 8,
                SiteBaseRequestsPerSecondMax = 10,
                SiteBaseRequestsIntervalMin = 10,
                SiteBaseRequestsIntervalMax = 20
            };

            StrategyHtmlEntity GreenWaterPipesProductNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h1.product-single__title",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity GreenWaterPipesProductImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/meta[16]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute GreenWaterPipesProductImageLink = new BaseItemStrategyAttribute
            {
                AttributeName = "content",
                AttributeAssingToRule = "imageUrl"
            };

            GreenWaterPipesProductImageSelector.AttributesList.Add(GreenWaterPipesProductImageLink);

            StrategyHtmlEntity GreenWaterPipesProductDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".product-description.rte",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity GreenWaterPipesProductShopifyMetaCodeSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyShopifyMetaScriptSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/script[15]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute GreenWaterPipesProductShopifyMetaCodeVendorAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "vendor",
                AttributeAssingToRule = "productVendor"
            };

            BaseItemStrategyAttribute GreenWaterPipesProductShopifyMetaCodeTypeAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "type",
                AttributeAssingToRule = "productCategory"
            };

            BaseItemStrategyAttribute GreenWaterPipesProductShopifyMetaCodeSkuAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "sku",
                AttributeAssingToRule = "productCode"
            };

            GreenWaterPipesProductShopifyMetaCodeSelector.AttributesList.Add(GreenWaterPipesProductShopifyMetaCodeVendorAttribute);
            GreenWaterPipesProductShopifyMetaCodeSelector.AttributesList.Add(GreenWaterPipesProductShopifyMetaCodeTypeAttribute);
            // GreenWaterPipesProductShopifyMetaCodeSelector.AttributesList.Add(GreenWaterPipesProductShopifyMetaCodeSkuAttribute);

            StrategyHtmlEntity GreenWaterPipesProductPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "#ProductPrice-product-template",
                AssignEntityTo = "productPrice"
            };

            StrategyHtmlEntity GreenWaterPipesProductOldPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "#ComparePrice-product-template",
                AssignEntityTo = "productSalePrice"
            };

            StrategyHtmlEntity GreenWaterPipesProductCodeSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".variant-sku",
                AssignEntityTo = "productCode"
            };

            StrategyHtmlEntity GreenWaterPipesProductInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".product-stock .hide",
                AssignEntityTo = "isProductInStock",
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "Unavailable",
                    ResultIfPassed = true,
                    ResultIfFailed = false
                }
            };

            GreenWaterPipes.ScrappingElements.Add(GreenWaterPipesProductNameSelector);
            GreenWaterPipes.ScrappingElements.Add(GreenWaterPipesProductImageSelector);
            GreenWaterPipes.ScrappingElements.Add(GreenWaterPipesProductDescriptionSelector);
            GreenWaterPipes.ScrappingElements.Add(GreenWaterPipesProductShopifyMetaCodeSelector);
            GreenWaterPipes.ScrappingElements.Add(GreenWaterPipesProductPriceSelector);
            GreenWaterPipes.ScrappingElements.Add(GreenWaterPipesProductOldPriceSelector);
            GreenWaterPipes.ScrappingElements.Add(GreenWaterPipesProductCodeSelector);
            GreenWaterPipes.ScrappingElements.Add(GreenWaterPipesProductInStockSelector);

            #endregion
            #region Cbdteas

            WebScrapperBaseSiteEntity Cbdteas = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://www.cbdteas.net/",
                BaseSiteUrl = "https://www.cbdteas.net/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ExternalHash = "?ref=weedrepublic",
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsAnother,
                SiteProductPageIndicationSelector = "h1.product-title span",
                DefaultBrand = "Buddha Teas",
                ScrappingElements = new List<StrategyHtmlEntity>(),
                ExcludeUrlsByParts = new List<string>(),
                SiteBaseRequestsPerSecondMin = 5,
                SiteBaseRequestsPerSecondMax = 8,
                SiteBaseRequestsIntervalMin = 2,
                SiteBaseRequestsIntervalMax = 6
            };

            StrategyHtmlEntity CbdteasProductNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h1.product-title span",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity CbdteasProductImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "#product_addtocart_form img",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute CbdteasProductImageLinkAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "src",
                AttributeAssingToRule = "imageUrl"
            };

            CbdteasProductImageSelector.AttributesList.Add(CbdteasProductImageLinkAttribute);

            StrategyHtmlEntity CbdteasProductDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".panel-body .std",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity CbdteasProductPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".price.special-price",
                AssignEntityTo = "productPrice"
            };

            StrategyHtmlEntity CbdteasProductOldPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".price.old-price",
                AssignEntityTo = "productSalePrice"
            };

            StrategyHtmlEntity CbdteasProductSchemaSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategySchemaOrgSelection,
                SelectionType = StrategyHtmlSelectionType.StrategyMultipleSelection,
                SelectByIndexFromRange = "64",
                BaseItemSelector = "script",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute CbdteasProductSchemaBrandAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "ProductBrand",
                AttributeAssingToRule = "productBrand"
            };

            BaseItemStrategyAttribute CbdteasProductSchemaSkuAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "SchemaSku",
                AttributeAssingToRule = "productCode"
            };

            CbdteasProductSchemaSelector.AttributesList.Add(CbdteasProductSchemaBrandAttribute);
            CbdteasProductSchemaSelector.AttributesList.Add(CbdteasProductSchemaSkuAttribute);

            StrategyHtmlEntity CbdteasProductInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".add-to-cart .button.btn-cart",
                AssignEntityTo = "isProductInStock",
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "Add to Cart",
                    ResultIfPassed = true,
                    ResultIfFailed = false
                }
            };

            Cbdteas.ScrappingElements.Add(CbdteasProductNameSelector);
            Cbdteas.ScrappingElements.Add(CbdteasProductImageSelector);
            Cbdteas.ScrappingElements.Add(CbdteasProductDescriptionSelector);
            Cbdteas.ScrappingElements.Add(CbdteasProductPriceSelector);
            Cbdteas.ScrappingElements.Add(CbdteasProductOldPriceSelector);
            Cbdteas.ScrappingElements.Add(CbdteasProductSchemaSelector);
            Cbdteas.ScrappingElements.Add(CbdteasProductInStockSelector);

            Cbdteas.ExcludeUrlsByParts.Add("/account/");
            Cbdteas.ExcludeUrlsByParts.Add("/wholesale/");
            Cbdteas.ExcludeUrlsByParts.Add("/contacts/");
            Cbdteas.ExcludeUrlsByParts.Add("/cbd-authenticity/");
            Cbdteas.ExcludeUrlsByParts.Add("/about-buddha-teas/");
            Cbdteas.ExcludeUrlsByParts.Add("/affiliates/");
            Cbdteas.ExcludeUrlsByParts.Add("/order/");
            Cbdteas.ExcludeUrlsByParts.Add("/payment/");
            Cbdteas.ExcludeUrlsByParts.Add("/shipping/");
            Cbdteas.ExcludeUrlsByParts.Add("/privacy-policy/");
            Cbdteas.ExcludeUrlsByParts.Add("/review/");
            Cbdteas.ExcludeUrlsByParts.Add(".jpg");
            Cbdteas.ExcludeUrlsByParts.Add(".png");
            Cbdteas.ExcludeUrlsByParts.Add(".jpeg");
            Cbdteas.ExcludeUrlsByParts.Add(".svg");

            #endregion
            #region Rcctools

            WebScrapperBaseSiteEntity Rcctools = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://rcctools.com/",
                BaseSiteUrl = "https://rcctools.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ExternalHash = "?wpam_id=3",
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsAnother,
                SiteProductPageIndicationSelector = "h1.product_title",
                ScrappingElements = new List<StrategyHtmlEntity>(),
                ExcludeUrlsByParts = new List<string>(),
                SiteBaseRequestsPerSecondMin = 5,
                SiteBaseRequestsPerSecondMax = 8,
                SiteBaseRequestsIntervalMin = 2,
                SiteBaseRequestsIntervalMax = 6,
                DefaultBrand = "Enail Rig Kits Customized and Bundled In The USA By RCCtools"
            };

            StrategyHtmlEntity RcctoolsProductNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h1.product_title",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity RcctoolsProductImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".wp-post-image",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute RcctoolsProductImageLinkAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "src",
                AttributeAssingToRule = "imageUrl"
            };

            RcctoolsProductImageSelector.AttributesList.Add(RcctoolsProductImageLinkAttribute);

            StrategyHtmlEntity RcctoolsProductDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".woocommerce-product-details__short-description",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity RcctoolsProductCategorySelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategyMultipleSelection,
                BaseItemSelector = ".woocommerce-breadcrumb a",
                AssignEntityTo = "productCategory",
                SelectByIndexFromRange = "2"
            };

            StrategyHtmlEntity RcctoolsProductInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".stock",
                AssignEntityTo = "isProductInStock",
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "In stock",
                    ResultIfFailed = false,
                    ResultIfPassed = true
                }
            };
            Rcctools.ScrappingElements.Add(RcctoolsProductNameSelector);
            Rcctools.ScrappingElements.Add(RcctoolsProductImageSelector);
            Rcctools.ScrappingElements.Add(RcctoolsProductDescriptionSelector);
            Rcctools.ScrappingElements.Add(RcctoolsProductInStockSelector);
            Rcctools.ScrappingElements.Add(RcctoolsProductCategorySelector);

            BaseWebDriverStrategy RcctoolsWebDriver = new BaseWebDriverStrategy
            {
                RequestUrl = "--current--",
                TasksList = new List<BaseWebDriverTaskStrategy>()
            };

            BaseWebDriverTaskStrategy RcctoolsWebDriverProductPriceSelection = new BaseWebDriverTaskStrategy
            {
                TaskType = BaseWebDriverTasksTypes.TaskGetDataFromPage,
                ScriptSourceType = WebScrapper.Scrapper.Entities.enums.By.FileSource,
                ScriptSource = "scripts/js/rcctools/TaskGetProductPrice.js",
                LoadDriverDependencies = true,
                AssignToField = "productPrice"
            };

            BaseWebDriverTaskStrategy RcctoolsWebDriverProductOldPriceSelection = new BaseWebDriverTaskStrategy
            {
                TaskType = BaseWebDriverTasksTypes.TaskGetDataFromPage,
                ScriptSourceType = WebScrapper.Scrapper.Entities.enums.By.FileSource,
                ScriptSource = "scripts/js/rcctools/TaskGetProductOldPrice.js",
                LoadDriverDependencies = true,
                AssignToField = "productSalePrice"
            };

            RcctoolsWebDriver.TasksList.Add(RcctoolsWebDriverProductPriceSelection);
            RcctoolsWebDriver.TasksList.Add(RcctoolsWebDriverProductOldPriceSelection);

            Rcctools.DriverStrategy = RcctoolsWebDriver;

            Rcctools.ExcludeUrlsByParts.Add("/product-tag/");
            Rcctools.ExcludeUrlsByParts.Add(".jpg");
            Rcctools.ExcludeUrlsByParts.Add(".jpeg");
            Rcctools.ExcludeUrlsByParts.Add(".png");
            Rcctools.ExcludeUrlsByParts.Add(".sgv");
            #endregion
            #region O2vape

            WebScrapperBaseSiteEntity O2vape = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://o2vape.com/product-sitemap.xml",
                BaseSiteUrl = "https://o2vape.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify, // in fact WP, but has similar sitemap
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 4,
                SiteBaseRequestsPerSecondMax = 6,
                SiteBaseRequestsIntervalMin = 10,
                SiteBaseRequestsIntervalMax = 30,
                ExcludeUrlsByParts = new List<string>(),
                DefaultBrand = "O2vape"
            };

            O2vape.ExcludeUrlsByParts.Add("/all-products/");

            StrategyHtmlEntity O2vapeProductNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h1.product_title",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity O2vapeProductImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".wp-post-image",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute O2vapeProductImageLinkAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "src",
                AttributeAssingToRule = "imageUrl"
            };

            O2vapeProductImageSelector.AttributesList.Add(O2vapeProductImageLinkAttribute);

            StrategyHtmlEntity O2vapeProductDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "#tab-description",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity O2vapeProductCategorySelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".prod_cats li a",
                AssignEntityTo = "productCategory"
            };

            StrategyHtmlEntity O2vapeProductSchemaSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategySchemaOrgSelection,
                SelectionType = StrategyHtmlSelectionType.StrategyMultipleSelection,
                SelectByIndexFromRange = "29",
                BaseItemSelector = "script",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute O2vapeProductSchemaSkuAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "sku",
                AttributeAssingToRule = "productCode"
            };

            O2vapeProductSchemaSelector.AttributesList.Add(O2vapeProductSchemaSkuAttribute);

            StrategyHtmlEntity O2vapeProductPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".woocommerce-Price-amount.amount",
                AssignEntityTo = "productPrice"
            };

            StrategyHtmlEntity O2vapeProductInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".stock",
                AssignEntityTo = "isProductInStock",
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Null",
                    ResultIfPassed = true,
                    ResultIfFailed = false
                }
            };

            O2vape.ScrappingElements.Add(O2vapeProductImageSelector);
            O2vape.ScrappingElements.Add(O2vapeProductNameSelector);
            O2vape.ScrappingElements.Add(O2vapeProductDescriptionSelector);
            O2vape.ScrappingElements.Add(O2vapeProductCategorySelector);
            // O2vape.ScrappingElements.Add(O2vapeProductSchemaSelector);
            O2vape.ScrappingElements.Add(O2vapeProductPriceSelector);
            O2vape.ScrappingElements.Add(O2vapeProductInStockSelector);

            O2vape.DriverStrategy = ShareAsaleDriverStrategy;

            #endregion
            #region Bloomgroove

            WebScrapperBaseSiteEntity Bloomgroove = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://bloomgroove.com/",
                BaseSiteUrl = "https://bloomgroove.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsAnother,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                ExcludeUrlsByParts = new List<string>(),
                SiteBaseRequestsPerSecondMin = 5,
                SiteBaseRequestsPerSecondMax = 8,
                SiteBaseRequestsIntervalMin = 20,
                SiteBaseRequestsIntervalMax = 30,
                SiteProductPageIndicationSelector = ".product-title h1"
            };

            StrategyHtmlEntity BloomgrooveProductNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".product-title h1",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity BloomgrooveProductImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/meta[10]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute BloomgrooveProductImageLinkAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "content",
                AttributeAssingToRule = "imageUrl"
            };

            BloomgrooveProductImageSelector.AttributesList.Add(BloomgrooveProductImageLinkAttribute);

            StrategyHtmlEntity BloomgrooveProductDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".product-description",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity BloomgrooveProductCategorySelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategyMultipleSelection,
                BaseItemSelector = ".breadcrumb ul li",
                SelectByIndexFromRange = "3",
                AssignEntityTo = "productCategory"
            };

            StrategyHtmlEntity BloomgrooveSchemaSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategySchemaOrgSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "script",
                AutoDetect = true,
                DetectType = "application/ld+json",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute BloomgrooveSchemaBrandAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "ProductBrand",
                AttributeAssingToRule = "productVendor"
            };

            BaseItemStrategyAttribute BloomgrooovesSchemaSkuAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "SchemaSku",
                AttributeAssingToRule = "productCode"
            };

            BaseItemStrategyAttribute BloomgrooveSchemaPriceAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "Price",
                AttributeAssingToRule = "productPrice"
            };

            BloomgrooveSchemaSelector.AttributesList.Add(BloomgrooveSchemaBrandAttribute);
            BloomgrooveSchemaSelector.AttributesList.Add(BloomgrooovesSchemaSkuAttribute);
            BloomgrooveSchemaSelector.AttributesList.Add(BloomgrooveSchemaPriceAttribute);


            StrategyHtmlEntity BloomgrooveProductInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".in-stock-container",
                AssignEntityTo = "isProductInStock",
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Null",
                    ResultIfPassed = false,
                    ResultIfFailed = true
                }
            };

            Bloomgroove.ScrappingElements.Add(BloomgrooveProductNameSelector);
            Bloomgroove.ScrappingElements.Add(BloomgrooveProductImageSelector);
            Bloomgroove.ScrappingElements.Add(BloomgrooveProductDescriptionSelector);
            Bloomgroove.ScrappingElements.Add(BloomgrooveProductCategorySelector);
            Bloomgroove.ScrappingElements.Add(BloomgrooveSchemaSelector);
            Bloomgroove.ScrappingElements.Add(BloomgrooveProductInStockSelector);

            Bloomgroove.DriverStrategy = ShareAsaleDriverStrategy;

            Bloomgroove.ExcludeUrlsByParts.Add("/review");
            Bloomgroove.ExcludeUrlsByParts.Add("/terms-of-use");
            Bloomgroove.ExcludeUrlsByParts.Add("/privacy-policy");
            Bloomgroove.ExcludeUrlsByParts.Add("/security");
            Bloomgroove.ExcludeUrlsByParts.Add("/customer-support");
            Bloomgroove.ExcludeUrlsByParts.Add("/refund-policy");
            Bloomgroove.ExcludeUrlsByParts.Add("/shipping-policy");
            Bloomgroove.ExcludeUrlsByParts.Add("/about");
            Bloomgroove.ExcludeUrlsByParts.Add("/affiliates");
            Bloomgroove.ExcludeUrlsByParts.Add("/sell-on-bloomgroove");
            Bloomgroove.ExcludeUrlsByParts.Add("?amp");
            Bloomgroove.ExcludeUrlsByParts.Add("?f=");
            Bloomgroove.ExcludeUrlsByParts.Add("page=");
            Bloomgroove.ExcludeUrlsByParts.Add(".jpg");
            Bloomgroove.ExcludeUrlsByParts.Add(".png");
            Bloomgroove.ExcludeUrlsByParts.Add(".jpeg");
            Bloomgroove.ExcludeUrlsByParts.Add(".svg");

            #endregion
            #region Funkyfarms

            WebScrapperBaseSiteEntity Funkyfarms = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://funkyfarms.com/sitemap_products_1.xml?from=1867192500329&to=4779486478441",
                BaseSiteUrl = "https://funkyfarms.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 4,
                SiteBaseRequestsPerSecondMax = 6,
                SiteBaseRequestsIntervalMin = 10,
                SiteBaseRequestsIntervalMax = 30,
                DefaultBrand = "Funky Farms",
                UseShareAsaleGeneration = true
            };

            StrategyHtmlEntity FunkyfarmsProductNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "div.product-name",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity FunkyfarmsProductImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "#product-featured-image",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute FunkyfarmsProductImageLinkAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "data-src",
                AttributeAssingToRule = "imageUrl"
            };

            FunkyfarmsProductImageSelector.AttributesList.Add(FunkyfarmsProductImageLinkAttribute);

            StrategyHtmlEntity FunkyframsProductDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".short-description",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity FunkyfarmsProductPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".price",
                AssignEntityTo = "productPrice"
            };

            StrategyHtmlEntity FunkyfaamsProductMetaSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyShopifyMetaScriptSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/script[18]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute FunkyfarmsProductMetaVendorAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "vendor",
                AttributeAssingToRule = "productVendor"
            };

            BaseItemStrategyAttribute FunkyfarmsProductMetaTypeAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "type",
                AttributeAssingToRule = "productCategory"
            };

            BaseItemStrategyAttribute FunkyfarmsProductMetaSkuAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "sku",
                AttributeAssingToRule = "productCode"
            };

            FunkyfaamsProductMetaSelector.AttributesList.Add(FunkyfarmsProductMetaVendorAttribute);
            FunkyfaamsProductMetaSelector.AttributesList.Add(FunkyfarmsProductMetaTypeAttribute);
            FunkyfaamsProductMetaSelector.AttributesList.Add(FunkyfarmsProductMetaSkuAttribute);

            StrategyHtmlEntity FunkyfarmsProductInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".addtocart",
                AssignEntityTo = "isProductInStock",
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "Add to Cart",
                    ResultIfPassed = true,
                    ResultIfFailed = false
                }
            };

            Funkyfarms.ScrappingElements.Add(FunkyframsProductDescriptionSelector);
            Funkyfarms.ScrappingElements.Add(FunkyfarmsProductNameSelector);
            Funkyfarms.ScrappingElements.Add(FunkyfarmsProductImageSelector);
            Funkyfarms.ScrappingElements.Add(FunkyfaamsProductMetaSelector);
            Funkyfarms.ScrappingElements.Add(FunkyfarmsProductPriceSelector);
            Funkyfarms.ScrappingElements.Add(FunkyfarmsProductInStockSelector);

            Funkyfarms.DriverStrategy = ShareAsaleDriverStrategy;

            #endregion
            #region Everyonedoesit

            WebScrapperBaseSiteEntity Everyonedoesit = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://www.everyonedoesit.com/sitemap_products_1.xml?from=10102421450&to=4668134916162",
                BaseSiteUrl = "https://www.everyonedoesit.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 8,
                SiteBaseRequestsPerSecondMax = 10,
                SiteBaseRequestsIntervalMin = 10,
                SiteBaseRequestsIntervalMax = 30,
                UseShareAsaleGeneration = true
            };

            StrategyHtmlEntity EveryonedoesitProductNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h1.tt-title",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity EveryonedoesitProductImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".zoom-product",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute EveryonedoesitProductImageLinkSelector = new BaseItemStrategyAttribute
            {
                AttributeName = "src",
                AttributeAssingToRule = "imageUrl"
            };

            EveryonedoesitProductImageSelector.AttributesList.Add(EveryonedoesitProductImageLinkSelector);

            StrategyHtmlEntity EveryonedoesitProductDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "#tt-tab-01",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity EveryonedoesitProductMetaSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyShopifyMetaScriptSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/script[10]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute EveryonedoesitProductMetaCodeAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "sku",
                AttributeAssingToRule = "productCode"
            };

            BaseItemStrategyAttribute EveryonedoesitProductMetaVendorAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "vendor",
                AttributeAssingToRule = "productVendor"
            };

            BaseItemStrategyAttribute EveryonedoesitProductMetaTypeAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "type",
                AttributeAssingToRule = "productCategory"
            };

            EveryonedoesitProductMetaSelector.AttributesList.Add(EveryonedoesitProductMetaCodeAttribute);
            EveryonedoesitProductMetaSelector.AttributesList.Add(EveryonedoesitProductMetaVendorAttribute);
            EveryonedoesitProductMetaSelector.AttributesList.Add(EveryonedoesitProductMetaTypeAttribute);

            StrategyHtmlEntity EveryonedoesitProductPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategyMultipleSelection,
                BaseItemSelector = ".tt-price",
                SelectByIndexFromRange = "2",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute EveryonedoesitProductPriceAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "content",
                AttributeAssingToRule = "productPrice"
            };

            EveryonedoesitProductPriceSelector.AttributesList.Add(EveryonedoesitProductPriceAttribute);

            StrategyHtmlEntity EveryonedoesitProductOldPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".old-price",
                AssignEntityTo = "productSalePrice"
            };

            StrategyHtmlEntity EveryonedoesitProductInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".tt-label-out-stock",
                AssignEntityTo = "isProductInStock",
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Null",
                    ComparedString = "Sold Out",
                    ResultIfPassed = true,
                    ResultIfFailed = false
                }
            };

            Everyonedoesit.ScrappingElements.Add(EveryonedoesitProductNameSelector);
            Everyonedoesit.ScrappingElements.Add(EveryonedoesitProductImageSelector);
            Everyonedoesit.ScrappingElements.Add(EveryonedoesitProductDescriptionSelector);
            Everyonedoesit.ScrappingElements.Add(EveryonedoesitProductMetaSelector);
            Everyonedoesit.ScrappingElements.Add(EveryonedoesitProductPriceSelector);
            Everyonedoesit.ScrappingElements.Add(EveryonedoesitProductOldPriceSelector);
            Everyonedoesit.ScrappingElements.Add(EveryonedoesitProductInStockSelector);

            // Everyonedoesit.DriverStrategy = ShareAsaleDriverStrategy;

            #endregion
            #region Premiumjane

            WebScrapperBaseSiteEntity Premiumjane = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://premiumjane.com/product-sitemap.xml",
                BaseSiteUrl = "https://premiumjane.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 1,
                SiteBaseRequestsPerSecondMax = 3,
                SiteBaseRequestsIntervalMin = 20,
                SiteBaseRequestsIntervalMax = 30,
                DefaultBrand = "PremiumJane",
                UseShareAsaleGeneration = true
            };

            StrategyHtmlEntity PremiumjaneProductNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h1.product_title",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity PremiumjaneProductImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".product__wrapper__primary__slider__images__image img",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute PremiumjaneProductImageLinkAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "data-src",
                AttributeAssingToRule = "imageUrl"
            };

            PremiumjaneProductImageSelector.AttributesList.Add(PremiumjaneProductImageLinkAttribute);

            StrategyHtmlEntity PremiumjaneProductDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "#tab-description",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity PremiumjaneProductCategorySelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".breadcrumbs__inner__line span span span span a",
                AssignEntityTo = "productCategory"
            };

            StrategyHtmlEntity PremiumjaneProductPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".woocommerce-Price-number",
                AssignEntityTo = "productPrice"
            };

            StrategyHtmlEntity PremiumjaneProductCodeSeelctor = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".product_sku",
                AssignEntityTo = "productCode"
            };

            StrategyHtmlEntity PremiumjaneProductInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".single_add_to_cart_button",
                AssignEntityTo = "isProductInStock",
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Null",
                    ResultIfPassed = false,
                    ResultIfFailed = true
                }
            };

            Premiumjane.ScrappingElements.Add(PremiumjaneProductNameSelector);
            Premiumjane.ScrappingElements.Add(PremiumjaneProductImageSelector);
            Premiumjane.ScrappingElements.Add(PremiumjaneProductDescriptionSelector);
            Premiumjane.ScrappingElements.Add(PremiumjaneProductCategorySelector);
            Premiumjane.ScrappingElements.Add(PremiumjaneProductPriceSelector);
            Premiumjane.ScrappingElements.Add(PremiumjaneProductCodeSeelctor);
            Premiumjane.ScrappingElements.Add(PremiumjaneProductInStockSelector);

            #endregion
            #region Cbdluxe

            WebScrapperBaseSiteEntity Cbdluxe = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://cbdluxe.com/product-sitemap.xml",
                BaseSiteUrl = "https://cbdluxe.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 1,
                SiteBaseRequestsPerSecondMax = 3,
                SiteBaseRequestsIntervalMin = 10,
                SiteBaseRequestsIntervalMax = 15,
                DefaultBrand = "CBD Luxe",
                DefaultStockValue = true,
                ExcludeUrlsByParts = new List<string>(),
                UseShareAsaleGeneration = true
            };

            Cbdluxe.ExcludeUrlsByParts.Add("/shop/");

            StrategyHtmlEntity CbdluxeProductNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h2.product_title",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity CbdluxeProductImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/meta[14]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute CbdluxeProductImageLinkAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "content",
                AttributeAssingToRule = "imageUrl"
            };

            CbdluxeProductImageSelector.AttributesList.Add(CbdluxeProductImageLinkAttribute);

            StrategyHtmlEntity CbdluxeProductDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "section.content .container",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity CbdluxeProductCategorySelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "span.posted_in a",
                AssignEntityTo = "productCategory"
            };

            StrategyHtmlEntity CbdluxeProductCodeSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "span.sku",
                AssignEntityTo = "productCode"
            };

            Cbdluxe.ScrappingElements.Add(CbdluxeProductNameSelector);
            Cbdluxe.ScrappingElements.Add(CbdluxeProductImageSelector);
            Cbdluxe.ScrappingElements.Add(CbdluxeProductDescriptionSelector);
            Cbdluxe.ScrappingElements.Add(CbdluxeProductCategorySelector);
            Cbdluxe.ScrappingElements.Add(CbdluxeProductCodeSelector);

            BaseWebDriverStrategy CbdluxeDriver = new BaseWebDriverStrategy
            {
                RequestUrl = "--current--",
                TasksList = new List<BaseWebDriverTaskStrategy>(),
                UseProxy = false,
                LaunchIncognito = true,
                LaunchHeadless = false,
                IgnoreCertificateErrors = true,
                DisableInfoBar = true
            };

            BaseWebDriverTaskStrategy CbdluxeDriverGetPriceTask = new BaseWebDriverTaskStrategy
            {
                TaskType = BaseWebDriverTasksTypes.TaskGetDataFromPage,
                ScriptSource = "scripts/js/cbdluxe/TaskGetCbdLuxePrice.js",
                ScriptSourceType = WebScrapper.Scrapper.Entities.enums.By.FileSource,
                AssignToField = "productPrice"
            };

            CbdluxeDriver.TasksList.Add(CbdluxeDriverGetPriceTask);

            Cbdluxe.DriverStrategy = CbdluxeDriver;
            #endregion
            #region Berkshirecbd

            WebScrapperBaseSiteEntity Berkshirecbd = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://berkshirecbd.com/",
                BaseSiteUrl = "https://berkshirecbd.com/product-sitemap.xml",
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 2,
                SiteBaseRequestsPerSecondMax = 5,
                SiteBaseRequestsIntervalMin = 20,
                SiteBaseRequestsIntervalMax = 30,
                DefaultBrand = "Berkshire CBD",
                UseShareAsaleGeneration = true
            };

            StrategyHtmlEntity BerkshirecbdProductNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".product--description h1",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity BerkshirecbdProductDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategyMultipleSelection,
                BaseItemSelector = ".product--description",
                AssignEntityTo = "productDescription",
                SelectByIndexFromRange = "2"
            };

            StrategyHtmlEntity BerkshirecbdProductImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".container.product--lifestyle-image.rocket-lazyload",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute BerkshirecbdProductImageLinkAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "data-bg",
                AttributeAssingToRule = "imageUrl"
            };

            BerkshirecbdProductImageSelector.AttributesList.Add(BerkshirecbdProductImageLinkAttribute);

            StrategyHtmlEntity BerkshirecbdProductGraphSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyGraphSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/script[1]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute BerkshirecbdProductGraphSkuAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "Sku",
                AttributeAssingToRule = "productCode"
            };

            BaseItemStrategyAttribute BerkshirecbdProductGraphCategoryAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "Category",
                AttributeAssingToRule = "productCategory"
            };

            BaseItemStrategyAttribute BerkshirecbdProductGraphPriceAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "Price",
                AttributeAssingToRule = "productPrice"
            };

            BaseItemStrategyAttribute BerkshirecbdProductGraphDescriptionAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "Description",
                AttributeAssingToRule = "productDescription"
            };

            BaseItemStrategyAttribute BerkshirecbdProductGraphInStockAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "IsProductInStock",
                AttributeAssingToRule = "isProductInStock"
            };

            BerkshirecbdProductGraphSelector.AttributesList.Add(BerkshirecbdProductGraphSkuAttribute);
            BerkshirecbdProductGraphSelector.AttributesList.Add(BerkshirecbdProductGraphCategoryAttribute);
            BerkshirecbdProductGraphSelector.AttributesList.Add(BerkshirecbdProductGraphPriceAttribute);
            BerkshirecbdProductGraphSelector.AttributesList.Add(BerkshirecbdProductGraphDescriptionAttribute);
            BerkshirecbdProductGraphSelector.AttributesList.Add(BerkshirecbdProductGraphInStockAttribute);

            Berkshirecbd.ScrappingElements.Add(BerkshirecbdProductNameSelector);
            Berkshirecbd.ScrappingElements.Add(BerkshirecbdProductDescriptionSelector);
            Berkshirecbd.ScrappingElements.Add(BerkshirecbdProductImageSelector);
            Berkshirecbd.ScrappingElements.Add(BerkshirecbdProductGraphSelector);

            #endregion
            #region Vapordna

            WebScrapperBaseSiteEntity Vapordna = new WebScrapperBaseSiteEntity
            {
                ItemUrl = "https://vapordna.com/sitemap_products_1.xml?from=1975177642043&to=5027939778619",
                BaseSiteUrl = "https://vapordna.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 2,
                SiteBaseRequestsPerSecondMax = 5,
                SiteBaseRequestsIntervalMin = 10,
                SiteBaseRequestsIntervalMax = 30,
                UseShareAsaleGeneration = true
            };

            StrategyHtmlEntity VapordnaProductNameSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h1.product-single__title",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity VapordnaPropductImageSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".photo-zoom-link.photo-zoom-link--enable",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute VapordnaPropductImageLinkAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "data-zoom-size",
                AttributeAssingToRule = "imageUrl"
            };

            VapordnaPropductImageSelector.AttributesList.Add(VapordnaPropductImageLinkAttribute);

            StrategyHtmlEntity VapordnaPropductDescriptionSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".easytabs-content-holder",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity VapordnaPropductMetaSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyShopifyMetaScriptSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/script[15]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute VapordnaPropductMetaSkuAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "sku",
                AttributeAssingToRule = "productCode"
            };

            BaseItemStrategyAttribute VapordnaPropductMetaVendorAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "vendor",
                AttributeAssingToRule = "productVendor"
            };

            BaseItemStrategyAttribute VapordnaPropductMetaTypeAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "type",
                AttributeAssingToRule = "productCategory"
            };

            VapordnaPropductMetaSelector.AttributesList.Add(VapordnaPropductMetaSkuAttribute);
            VapordnaPropductMetaSelector.AttributesList.Add(VapordnaPropductMetaVendorAttribute);
            VapordnaPropductMetaSelector.AttributesList.Add(VapordnaPropductMetaTypeAttribute);

            StrategyHtmlEntity VapordnaProductOldPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = ".product__price--compare",
                AssignEntityTo = "productSalePrice"
            };

            StrategyHtmlEntity VapordnaProductPriceSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "/html/head/meta[15]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute VapordnaProductPriceContentAttribute = new BaseItemStrategyAttribute
            {
                AttributeName = "content",
                AttributeAssingToRule = "productPrice"
            };

            VapordnaProductPriceSelector.AttributesList.Add(VapordnaProductPriceContentAttribute);

            StrategyHtmlEntity VapordnaProductInStockSelector = new StrategyHtmlEntity
            {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "#variant-inventory span b",
                AssignEntityTo = "isProductInStock",
                ValidationRule = new BaseHtmlItemStrategyValidationRule
                {
                    ValidationRule = "Is.Equals",
                    ComparedString = "Out of Stock",
                    ResultIfPassed = false,
                    ResultIfFailed = true
                }
            };

            Vapordna.ScrappingElements.Add(VapordnaProductNameSelector);
            Vapordna.ScrappingElements.Add(VapordnaPropductImageSelector);
            Vapordna.ScrappingElements.Add(VapordnaPropductDescriptionSelector);
            Vapordna.ScrappingElements.Add(VapordnaPropductMetaSelector);
            Vapordna.ScrappingElements.Add(VapordnaProductOldPriceSelector);
            Vapordna.ScrappingElements.Add(VapordnaProductPriceSelector);
            Vapordna.ScrappingElements.Add(VapordnaProductInStockSelector);

            #endregion

            #region Purerelief

            WebScrapperBaseSiteEntity Purerelief = new WebScrapperBaseSiteEntity { 
                ItemUrl = "https://www.purerelief.com/product-sitemap.xml",
                BaseSiteUrl = "https://www.purerelief.com/",
                ProductFamily = settings.ApplicationBaseProductFamilyType,
                SitePlatform = WebScrapperSiteTypes.SitePlatformIsShopify,
                ScrappingElements = new List<StrategyHtmlEntity>(),
                SiteBaseRequestsPerSecondMin = 2,
                SiteBaseRequestsPerSecondMax = 5,
                SiteBaseRequestsIntervalMin = 10,
                SiteBaseRequestsIntervalMax = 30,
                UseShareAsaleGeneration = true,
                DefaultBrand = "PURE RELIEF"
            };

            StrategyHtmlEntity PurereliefProductNameSelector = new StrategyHtmlEntity { 
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "h1.product_title",
                AssignEntityTo = "productName"
            };

            StrategyHtmlEntity PurereliefProductImageSelector = new StrategyHtmlEntity { 
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategyMultipleSelection,
                BaseItemSelector = "/html/head/meta",
                AutoDetect = true,
                DetectType = "property=og:image",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute PurereliefProductImageLinkAttribute = new BaseItemStrategyAttribute { 
                AttributeName = "content",
                AttributeAssingToRule = "imageUrl"
            };

            PurereliefProductImageSelector.AttributesList.Add(PurereliefProductImageLinkAttribute);

            StrategyHtmlEntity PurereliefProductDescriptionSelector = new StrategyHtmlEntity { 
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection,
                SelectionType = StrategyHtmlSelectionType.StrategyMultipleSelection,
                SelectByIndexFromRange = "6",
                BaseItemSelector = "section.elementor-element",
                AssignEntityTo = "productDescription"
            };

            StrategyHtmlEntity PurereliefProductCategorySelector = new StrategyHtmlEntity {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategyMultipleSelection,
                SelectByIndexFromRange = "prelast",
                BaseItemSelector = ".breadcrumbs span",
                AssignEntityTo = "productCategory"
            };

            StrategyHtmlEntity PurereliefProductCodeSelector = new StrategyHtmlEntity { 
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "input[name=gtm4wp_sku]",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute PurereliefProductCodeValueAttribute = new BaseItemStrategyAttribute { 
                AttributeName = "value",
                AttributeAssingToRule = "productCode"
            };

            PurereliefProductCodeSelector.AttributesList.Add(PurereliefProductCodeValueAttribute);

            StrategyHtmlEntity PurereliefProductPriceSelector = new StrategyHtmlEntity { 
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategyMultipleSelection,
                BaseItemSelector = "/html/head/meta",
                AutoDetect = true,
                DetectType = "property=product:price:amount",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute PurereliefProductPriceContentAttribute = new BaseItemStrategyAttribute { 
                AttributeName = "content",
                AttributeAssingToRule = "productPrice"
            };

            PurereliefProductPriceSelector.AttributesList.Add(PurereliefProductPriceContentAttribute);

            StrategyHtmlEntity PurereliefProductOldPriceSelector = new StrategyHtmlEntity {
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyInnerTextSelection,
                SelectionType = StrategyHtmlSelectionType.StrategySingularSelection,
                BaseItemSelector = "p.price del .woocommerce-Price-amount",
                AssignEntityTo = "productSalePrice"
            };

            StrategyHtmlEntity PurereliefProductInStockSelector = new StrategyHtmlEntity { 
                SelectionStrategy = ScrapperHtmlStrategyTypes.StrategyAttributesSelection,
                SelectionType = StrategyHtmlSelectionType.StrategyMultipleSelection,
                BaseItemSelector = "/html/head/meta",
                AutoDetect = true,
                DetectType = "property=product:availability",
                AttributesList = new List<BaseItemStrategyAttribute>()
            };

            BaseItemStrategyAttribute PurereliefProductInStockContentAttribute = new BaseItemStrategyAttribute { 
                AttributeName = "content",
                AttributeAssingToRule = "isProductInStock",
                AttributeValidationRule = new BaseHtmlItemStrategyValidationRule { 
                    ValidationRule = "Is.Equals",
                    ComparedString = "instock",
                    ResultIfPassed = true,
                    ResultIfFailed = false
                }
            };

            PurereliefProductInStockSelector.AttributesList.Add(PurereliefProductInStockContentAttribute);

            Purerelief.ScrappingElements.Add(PurereliefProductPriceSelector);
            Purerelief.ScrappingElements.Add(PurereliefProductNameSelector);
            Purerelief.ScrappingElements.Add(PurereliefProductImageSelector);
            Purerelief.ScrappingElements.Add(PurereliefProductDescriptionSelector);
            Purerelief.ScrappingElements.Add(PurereliefProductCategorySelector);
            Purerelief.ScrappingElements.Add(PurereliefProductCodeSelector);
            Purerelief.ScrappingElements.Add(PurereliefProductOldPriceSelector);
            Purerelief.ScrappingElements.Add(PurereliefProductInStockSelector);

            #endregion

            #region Shopify scrapping instances

            // BaseRequestList.Add(BadassGlass);
            // BaseRequestList.Add(TokerSupply);
            // BaseRequestList.Add(Oozelife);
            // BaseRequestList.Add(DrDabber);
            // BaseRequestList.Add(PuffingBird);
            // BaseRequestList.Add(Dankgeek);
            // BaseRequestList.Add(TransendLabs);
            // BaseRequestList.Add(PhilterLabs);
            // BaseRequestList.Add(SolCbd);
            // BaseRequestList.Add(SmellVeil);
            // BaseRequestList.Add(SlickVapes);
            // BaseRequestList.Add(BuyCbdCigarettes);
            // BaseRequestList.Add(BrothersWithGlass);
            // BaseRequestList.Add(Getmax);
            // BaseRequestList.Add(Ismokefresh);
            // BaseRequestList.Add(TankGlass);
            // BaseRequestList.Add(GreenWaterPipes);
            // BaseRequestList.Add(O2vape);
            // BaseRequestList.Add(Funkyfarms);
            // BaseRequestList.Add(Everyonedoesit);
            // BaseRequestList.Add(Premiumjane);
            // BaseRequestList.Add(Cbdluxe);
            // BaseRequestList.Add(Berkshirecbd);
            // BaseRequestList.Add(Vapordna);
            BaseRequestList.Add(Purerelief);

            #endregion
            #region Other scrapping instances
            // BaseRequestList.Add(CdbCo);
            // BaseRequestList.Add(GrassSity);
            // BaseRequestList.Add(CbdResellers);
            // BaseRequestList.Add(CbdNaturals);
            // BaseRequestList.Add(CbdPure);
            // BaseRequestList.Add(Cbdvapejuice);
            // BaseRequestList.Add(Cbdteas);
            // BaseRequestList.Add(Rcctools);
            // BaseRequestList.Add(Bloomgroove);
            #endregion


            var _b = new ScrapingBrowser();
            var _n = _b.NavigateToPage(new Uri("https://www.purerelief.com/500-mg-oil-bundle/")).Html;
            new BaseScrapper(_n, new WebScrapperBaseProxyEntity{}, _logger, "https://www.purerelief.com/500-mg-oil-bundle/").ScrappingInstance(Purerelief); 
        }
        public void OnScrapperUpdatingStatus(object sender, BaseScrapperChangeStatusCallbackResult eventArgs)
        {
            try
            {
                var selected = BaseRequestList.FirstOrDefault(e => e.BaseSiteUrl.Equals(eventArgs.BaseSiteUrl));
                if (!ReferenceEquals(selected, null))
                {
                    selected.SiteStatus = eventArgs.SiteStatus;
                }
                else
                {
                    throw new Exception();
                }

            }
            catch (Exception)
            {
                _logger.error($"Cannot find instance, satisfied to url {eventArgs.BaseSiteUrl}");
            }
        }
    }
}