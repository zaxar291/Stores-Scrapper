using System;
using System.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;

using Newtonsoft.Json;

using ScrapySharp.Network;
using ScrapySharp.Extensions;
using HtmlAgilityPack;

using WebApplication.Scrapper.Abstraction;
using WebApplication.Scrapper.Entities.BaseScrapperEntities;
using WebApplication.Scrapper.Entities.StrategiesEntities;
using WebApplication.Scrapper.Entities;
using WebApplication.Scrapper.Entities.enums;
using WebApplication.Scrapper.Services.Akeneo.Entities;
using WebApplication.Scrapper.Services;

using WebScrapper.Scrapper.Abstraction;
using WebScrapper.Scrapper.Implementation.Driver;
using WebScrapper.Scrapper.Entities.StrategiesEntities.Driver;
using WebScrapper.Scrapper.Entities;
using WebScrapper.Scrapper.Entities.enums;
using WebScrapper.Scrapper.Entities.ShopifyEntities;
using WebScrapper.Scrapper.Entities.BaseScrapperEntities;

using OpenQA.Selenium.Chrome;

namespace WebApplication.Scrapper.Implementation
{
    public class BaseScrapper : AbstractBaseScrapper<HtmlNode, WebScrapperBaseSiteEntity>
    {
        #region Private fields
        private AkeneoProduct AkeneoProduct { get; set; }
        private BindingFlags BindingFlags { get; set; }
        private string ProductUrl { get; set; }
        private AbstractWriter _f { get; set; }
        private WebScrapperBaseProxyEntity _ps { get; set; }
        #endregion

        #region Protected fields
        protected override HtmlNode BrowserNode { get; set; }
        protected override BaseNodeHandler<HtmlNode> NodeHandler { get; set; }
        protected override BaseLogger _l { get; set; }
        protected IBaseScrapperValidationService<BaseHtmlItemStrategyValidationRule> ValidationService { get; set; }
        #endregion

        #region Public members
        public BaseScrapper(HtmlNode Node, WebScrapperBaseProxyEntity _pr, BaseLogger logger, string productUrl)
        {
            BrowserNode = Node;
            NodeHandler = new WebScrapperNodeHandler(Node);
            _l = logger;
            _l.info("Base scrapper node -> starting...");
            AkeneoProduct = new AkeneoProduct();
            BindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            ValidationService = new BaseScrapperEntitiesValidator();
            ProductUrl = productUrl;
            _f = new FilesWriter();
            _ps = _pr;
        }

        public override AkeneoProduct ScrappingInstance(WebScrapperBaseSiteEntity ScrappingStrategy)
        {
            _l.info("Base scrapper node -> starting scrapping instance...");
            if (ReferenceEquals(ScrappingStrategy, null))
            {
                throw new Exception("Empty scrapping strategy presented to the BaseScrapper!");
            }
            if (ReferenceEquals(ScrappingStrategy.ProductFamily, null)
                || ScrappingStrategy.ProductFamily.Equals(String.Empty))
            {
                throw new Exception("Product family cannot be empty!");
            }
            if (ReferenceEquals(ScrappingStrategy.ScrappingElements, null)
                || ScrappingStrategy.ScrappingElements.Count.Equals(0))
            {
                throw new Exception("Any elements present for the scrapping process!");
            }
            if (ReferenceEquals(BrowserNode, null))
            {
                throw new Exception("Browser node null error! Stopping scrapping process..");
            }
            if (ReferenceEquals(ScrappingStrategy.ExternalHash, null))
            {
                AkeneoProduct.productUrl = $"{ProductUrl}";
            }
            else
            {
                AkeneoProduct.productUrl = $"{ProductUrl}{ScrappingStrategy.ExternalHash}";
            }

            AkeneoProduct.productFamily = ScrappingStrategy.ProductFamily;
            AkeneoProduct.productId = GenerateRandomProductIdentifier();
            if (!ReferenceEquals(ScrappingStrategy.DefaultStockValue, null))
            {
                AkeneoProduct.isProductInStock = ScrappingStrategy.DefaultStockValue;
            }
            if (!ReferenceEquals(ScrappingStrategy.DefaultBrand, null))
            {
                AkeneoProduct.productVendor = ScrappingStrategy.DefaultBrand;
            }
            var a = BaseScrappMultiple(".easytabs-content-holder");
            ScrapCollectionByNode(ScrappingStrategy);
            if (ScrappingStrategy.ItemUrl.Equals("https://cbd.co/"))
            {
                string name = NodeHandler.SelectSingleElement("h1.productView-title").InnerText;
                string[] exploded = name.Split("-");
                AkeneoProduct.productCategory = exploded[1].Trim();
            }
            if (!ReferenceEquals(ScrappingStrategy.DriverStrategy, null))
            {
                ChromeOptions options = new ChromeOptions();
                options.AddArgument("ignore-certificate-errors");
                options.AddArgument("disable-infobars");
                // options.AddArgument("--incognito");
                options.AddArgument("--headless");
                var currentUrl = "";
                if (ScrappingStrategy.DriverStrategy.RequestUrl.Equals("--current--"))
                {
                    currentUrl = ProductUrl;
                }
                else
                {
                    currentUrl = ScrappingStrategy.DriverStrategy.RequestUrl;
                }
                IWebDriverResolver WebDriver = new ChromeDriverResolver(ScrappingStrategy.DriverStrategy, _ps, currentUrl, _l); ;
                try
                {
                    ProcessWebDriverEntities(WebDriver, ScrappingStrategy.DriverStrategy);
                }
                catch (Exception e)
                {
                    _l.error(e.Message);
                }
                finally
                {
                    WebDriver?.Dispose();
                }
            }
            return AkeneoProduct;
        }
        #endregion

        #region Protected members

        #region Protected overrided void(s)
        protected override void ScrapCollectionByNode(WebScrapperBaseSiteEntity ScrappingStrategy)
        {
            var ScrappedResult = String.Empty;
            foreach (var strategy in ScrappingStrategy.ScrappingElements)
            {
                switch (strategy.SelectionStrategy)
                {
                    case ScrapperHtmlStrategyTypes.StrategyInnerTextSelection:
                        ScrappedResult = ScrapByInnerText(strategy);
                        break;
                    case ScrapperHtmlStrategyTypes.StrategyInnerHtmlSelection:
                        ScrappedResult = ScrapByInnerHtml(strategy);
                        break;
                    case ScrapperHtmlStrategyTypes.StrategyAttributesSelection:
                        ScrapByAttributes(strategy);
                        ScrappedResult = String.Empty;
                        break;
                    case ScrapperHtmlStrategyTypes.StrategySchemaOrgSelection:
                        ScrapBySchemaOrg(strategy);
                        ScrappedResult = String.Empty;
                        break;
                    case ScrapperHtmlStrategyTypes.StrategyShopifyMetaScriptSelection:
                        ScrapByShopifyMetaCode(strategy);
                        ScrappedResult = String.Empty;
                        break;
                    case ScrapperHtmlStrategyTypes.StrategyPuffingBirdTags:
                        ScrapByShopifyDeliveryJs(strategy);
                        ScrappedResult = String.Empty;
                        break;
                    case ScrapperHtmlStrategyTypes.StrategyShopifyJsonProductTemplate:
                        ScrapByProductJsTemplate(strategy);
                        ScrappedResult = String.Empty;
                        break;
                    case ScrapperHtmlStrategyTypes.StrategyGrassCityJs:
                        ScrapByGrassCityJs(strategy);
                        ScrappedResult = String.Empty;
                        break;
                    case ScrapperHtmlStrategyTypes.StrategyShopifyProductJsonTemplate:
                        ScrappByShopifyProductJsonTemplate(strategy);
                        ScrappedResult = String.Empty;
                        break;
                    case ScrapperHtmlStrategyTypes.StrategyGraphSelection:
                        ScrapByGraphTemplate(strategy);
                        ScrappedResult = String.Empty;
                        break;
                    default:
                        throw new Exception($"Unknown strategy presented to the scrapping process! Current strategy is: {strategy.SelectionStrategy}");
                }
                if (!ReferenceEquals(ScrappedResult, null))
                {
                    ScrappedResult = ScrappedResult.Trim();
                }

                //ToDo: remove
                if (!ReferenceEquals(strategy.AssignEntityTo, null)
                    && strategy.AssignEntityTo.Equals("productName"))
                {
                    ScrappedResult = WebUtility.HtmlDecode(ScrappedResult);
                }
                if (!ReferenceEquals(strategy.AssignEntityTo, null)
                    && strategy.AssignEntityTo.Equals("productDescription")
                    && !ReferenceEquals(ScrappedResult, null))
                {

                    ScrappedResult = WebUtility.HtmlDecode(ScrappedResult);
                    ScrappedResult =
                        Regex.Replace(ScrappedResult, @"<a", "<p");
                    ScrappedResult =
                        Regex.Replace(ScrappedResult, @"</a>", "</p>");
                    ScrappedResult =
                        Regex.Replace(ScrappedResult, @"href", "data-link");
                }
                if (!ReferenceEquals(strategy.ValidationRule, null))
                {
                    if (ValidationService.ValidateByRule(ScrappedResult, strategy.ValidationRule))
                    {
                        ScrappedResult = strategy.ValidationRule.ResultIfPassed.ToString();
                    }
                    else
                    {
                        ScrappedResult = strategy.ValidationRule.ResultIfFailed.ToString();
                    }
                }
                if (!ReferenceEquals(ScrappedResult, null) && !ScrappedResult.Equals(String.Empty))
                {
                    if (SetByReflectMethod(ScrappedResult, strategy.AssignEntityTo))
                    {
                        _l.info($"Strategy {strategy.BaseItemSelector} successfully processed!");
                    }
                    else
                    {
                        _l.error($"Some errors in strategy {strategy.BaseItemSelector} detected, you can find more inforamtion in aplication error logs");
                    }
                }
            }
        }

        #endregion

        #region Protected void(s)
        protected void ScrapByAttributes(StrategyHtmlEntity strategy)
        {
            if (!ReferenceEquals(strategy.AttributesList, null) && strategy.AttributesList.Count > 0)
            {
                if (strategy.SelectionType.Equals(StrategyHtmlSelectionType.StrategySingularSelection))
                {
                    var e = BaseScrapp(strategy.BaseItemSelector);
                    if (ReferenceEquals(e, null))
                    {
                        _l.warn($"Nothing scrapped from node {strategy.BaseItemSelector}");
                        return;
                    }
                    ProcessAttributesList(e, strategy);
                }
                else
                {
                    var e = BaseScrappMultiple(strategy.BaseItemSelector);
                    if (ReferenceEquals(e, null) || e.Count == 0)
                    {
                        _l.warn($"Nothing scrapped from node {strategy.BaseItemSelector}");
                        return;
                    }
                    if (!ReferenceEquals(strategy.AutoDetect, null) && strategy.AutoDetect)
                    {
                        var detectionParts = strategy.DetectType.Split("=");
                        var detectionAttribute = detectionParts[0];
                        var detectionValue = detectionParts[1];
                        foreach (var item in e)
                        {
                            var _content = item.GetAttributeValue(detectionAttribute);
                            if (!ReferenceEquals(_content, null) && !ReferenceEquals(_content, String.Empty))
                            {
                                if (_content.ToLower().Trim() == detectionValue.ToLower().Trim())
                                {
                                    ProcessAttributesList(item, strategy);
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!ReferenceEquals(strategy.SelectByIndexFromRange, null))
                        {
                            int rangeIdentifier = 0;
                            int currentPosition = 0;
                            if (strategy.SelectByIndexFromRange.Equals("prelast"))
                            {
                                rangeIdentifier = e.Count - 2;
                            }
                            else
                            {
                                rangeIdentifier = int.Parse(strategy.SelectByIndexFromRange) - 1;
                            }
                            foreach (var n in e)
                            {
                                if (currentPosition < rangeIdentifier)
                                {
                                    currentPosition++;
                                    continue;
                                }
                                ProcessAttributesList(n, strategy);
                                break;
                            }
                        }
                        else
                        {
                            foreach (var n in e)
                            {
                                ProcessAttributesList(n, strategy);
                            }
                        }
                    }
                }
            }
        }
        protected void ProcessAttributesList(HtmlNode elementNode, StrategyHtmlEntity strategy)
        {
            foreach (var attribute in strategy.AttributesList)
            {
                try
                {
                    var attributeValue = elementNode.GetAttributeValue(attribute.AttributeName);
                    if (!ReferenceEquals(attribute.AttributeValidationRule, null))
                    {
                        if (ValidationService.ValidateByRule(attributeValue, attribute.AttributeValidationRule))
                        {
                            attributeValue = attribute.AttributeValidationRule.ResultIfPassed.ToString();
                        }
                        else
                        {
                            attributeValue = attribute.AttributeValidationRule.ResultIfFailed.ToString();
                        }
                    }
                    if (!attributeValue.Equals(String.Empty))
                    {
                        if (SetByReflectMethod(attributeValue, attribute.AttributeAssingToRule))
                        {
                            _l.info($"Successfully created value from element {strategy.BaseItemSelector} with attribute {attribute.AttributeName}");
                        }
                        else
                        {
                            _l.error($"Some errors during creating value from element {strategy.BaseItemSelector} with attribute {attribute.AttributeName}, you can find more information in application error logs.");
                        }
                    }
                    else
                    {
                        _l.warn($"Empty attribute value form node {strategy.BaseItemSelector} with attribute {attribute.AttributeName}");
                    }
                }
                catch (Exception e)
                {
                    _l.warn($"Cannot select attribute {attribute.AttributeName} from element {strategy.BaseItemSelector} : {e.Message} -> {e.StackTrace}");
                }
            }
        }
        protected void ScrappByShopifyProductJsonTemplate(StrategyHtmlEntity strategy)
        {
            if (ReferenceEquals(strategy.AttributesList, null) || ReferenceEquals(strategy.AttributesList.Count, 0))
            {
                _l.warn($"Strategy {strategy.BaseItemSelector} doesn't contains any attribute list for processing! Site url: {ProductUrl}");
                return;
            }
            var node = BaseScrapp(strategy.BaseItemSelector);
            if (ReferenceEquals(node, null))
            {
                return;
            }
            try
            {
                var schema = JsonConvert.DeserializeObject<BaseScrapperShopifyProductTemplate>(node.InnerText);
                var productSchema = schema.product;
                int tagsLimit = 3;
                int currentTag = 1;
                var temp = new List<string>();
                foreach (var tag in productSchema.ProductTags)
                {
                    if (currentTag > tagsLimit)
                    {
                        break;
                    }
                    temp.Add(tag);
                    currentTag++;
                }
                productSchema.ProductLimitedTags = String.Join(",", temp);
                var describedSchema = productSchema.GetType().GetFields(BindingFlags);
                foreach (var attr in strategy.AttributesList)
                {
                    foreach (var field in describedSchema)
                    {
                        if (field.Name.Contains(attr.AttributeName))
                        {
                            if (SetByReflectMethod(field.GetValue(productSchema).ToString(), attr.AttributeAssingToRule))
                            {
                                _l.info($"Schema successfully processed!");
                            }
                            else
                            {
                                _l.error($"Error in schema processing, you can find more information in application error logs");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _l.error($"Shopify json template: fatal -> {e.Message} : {e.StackTrace}");
                return;
            }
        }

        #endregion

        #region Protected strings
        protected string ScrapBySchemaOrg(StrategyHtmlEntity entity)
        {
            var collection = BaseScrappMultiple(entity.BaseItemSelector);
            if (ReferenceEquals(collection, null))
            {
                return String.Empty;
            }
            HtmlNode SchemaRequested = null;
            if (!ReferenceEquals(entity.AutoDetect, null)
                && entity.AutoDetect
                && !ReferenceEquals(entity.DetectType, null)
                && !ReferenceEquals(entity.DetectType, String.Empty))
            {
                foreach (var script in collection)
                {
                    if (script.OuterHtml.Contains(entity.DetectType))
                    {
                        SchemaRequested = script;
                        break;
                    }
                }
            }
            else
            {
                var counter = 0;
                foreach (var item in collection)
                {
                    if (counter < int.Parse(entity.SelectByIndexFromRange))
                    {
                        counter++;
                        continue;
                    }
                    SchemaRequested = item;
                    break;
                }
            }
            if (ReferenceEquals(SchemaRequested, null))
            {
                return String.Empty;
            }

            try
            {
                var preparedContext = Regex.Replace(SchemaRequested.InnerHtml.Trim().Replace("\n", ""), "\\\"aggregateRating\\\":.+}, ", "");
                preparedContext = Regex.Replace(preparedContext, "\\\"image\\\":.+]", "\"image\":\"\"");
                var schemaEntity = JsonConvert.DeserializeObject<BaseScrapperSchemaEntity>(preparedContext);
                if (ReferenceEquals(entity.AttributesList, null) || entity.AttributesList.Count.Equals(0))
                {
                    _l.error($"Error in processing schema from url {ProductUrl} : emprty attributes list, nothing to fill!");
                    return String.Empty;
                }
                if (ReferenceEquals(schemaEntity.SchemaSku, null))
                {
                    schemaEntity.SchemaSku = String.Empty;
                }
                if (!ReferenceEquals(schemaEntity.SchemaBrand, null) && !ReferenceEquals(schemaEntity.SchemaBrand.FieldValue, null))
                {
                    schemaEntity.ProductBrand = schemaEntity.SchemaBrand.FieldValue;
                }
                if (!ReferenceEquals(schemaEntity.Offer, null) && !ReferenceEquals(schemaEntity.Offer.Price, null))
                {
                    schemaEntity.Price = schemaEntity.Offer.Price;
                }
                var describedSchema = schemaEntity.GetType().GetFields(BindingFlags);
                foreach (var attr in entity.AttributesList)
                {
                    foreach (var schemaField in describedSchema)
                    {
                        if (schemaField.Name.Contains(attr.AttributeName))
                        {
                            if (SetByReflectMethod(schemaField.GetValue(schemaEntity).ToString(), attr.AttributeAssingToRule))
                            {
                                _l.info($"Schema successfully processed!");
                            }
                            else
                            {
                                _l.error($"Error in schema processing, you can find more information in application error logs");
                            }
                        }
                    }
                }
                return collection.ToString();
            }
            catch (Exception e)
            {
                _l.error($"Error in processing schema from url {ProductUrl} : {e.Message} -> {e.StackTrace}");
                return String.Empty;
            }
        }
        protected string ScrapByShopifyMetaCode(StrategyHtmlEntity entity)
        {
            var t = BaseScrappMultiple("/html/head/script");
            _l.info($"Processing ScrapByShopifyMetaCode: for site {ProductUrl}");
            if (ReferenceEquals(entity, null))
            {
                _l.error($"Error in strategy for site {ProductUrl}");
                return String.Empty;
            }
            var ScriptHaystack = BaseScrapp(entity.BaseItemSelector);
            if (ReferenceEquals(ScriptHaystack, null))
            {
                _l.error($"Nothing to select from node {entity.BaseItemSelector}");
                return String.Empty;
            }
            string JsonResponse = Regex.Match(ScriptHaystack.InnerHtml, @"{.+}}", RegexOptions.IgnoreCase).Value;
            try
            {
                var schema = JsonConvert.DeserializeObject<ShopifySiteSchemaResponse>(JsonResponse);
                if (!ReferenceEquals(schema, null) && !ReferenceEquals(schema.product, null) && !ReferenceEquals(schema.product.variants, null) && schema.product.variants.Count > 0)
                {
                    foreach (var p in schema.product.variants)
                    {
                        schema.product.sku = p.sku;

                    }
                }
                if (schema.product.type.Contains(","))
                {
                    var cats = schema.product.type.Split(",");
                    schema.product.type = cats.FirstOrDefault();
                }
                foreach (var attribute in entity.AttributesList)
                {
                    var describedSchema = schema.product.GetType().GetFields(BindingFlags);
                    if (!ReferenceEquals(describedSchema, null))
                    {
                        foreach (var field in describedSchema)
                        {
                            if (field.Name.Contains(attribute.AttributeName))
                            {
                                var value = field.GetValue(schema.product).ToString();
                                if (SetByReflectMethod(value, attribute.AttributeAssingToRule))
                                {
                                    _l.info($"Schema successfully processed!");
                                }
                                else
                                {
                                    _l.error($"Schema cannot be processed, some errors occured, you can find more info in aplication error logs.");
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception e)
            {
                _l.error($"Error in deconverting json {ProductUrl}: {e.Message} -> {e.StackTrace}");
            }
            return String.Empty;
        }
        #endregion

        #endregion

        #region Chrome driver handler
        protected override void ProcessWebDriverEntities(IWebDriverResolver WebDriver, BaseWebDriverStrategy strategy)
        {
            if (ReferenceEquals(strategy.TasksList, null) || strategy.TasksList.Count.Equals(0))
            {
                _l.error("Webdriver not started: are you forgot to create driver tasks?");
                return;
            }
            if (!WebDriver.Initialize())
            {
                _l.error("Driver initialization fatal error, you can find more information in error logs");
                return;
            }
            try
            {
                foreach (var executableTask in strategy.TasksList)
                {
                    if (executableTask.TaskType.Equals(BaseWebDriverTasksTypes.TaskUpdateFieldData))
                    {
                        if (!ReferenceEquals(executableTask.ObtainFromField, null))
                        {
                            var temp = GetByReflectMethod(executableTask.ObtainFromField);
                            if (!ReferenceEquals(temp, null) && !ReferenceEquals(temp, String.Empty))
                            {
                                executableTask.NewValue = temp;
                            }
                        }
                        if (WebDriver.UpdateFieldData(executableTask.RequestElement, executableTask.NewValue))
                        {
                            _l.info($"Task: updating data in field {executableTask.RequestElement} success!");
                        }
                        else
                        {
                            _l.error("Webdriver: fatal error, tasks execution failed.");
                            return;
                        }
                    }
                    else if (executableTask.TaskType.Equals(BaseWebDriverTasksTypes.TaskExecuteScript))
                    {
                        if (WebDriver.ExecuteScript(executableTask.ScriptSource))
                        {
                            _l.info("Task: executing script, success!");
                        }
                        else
                        {
                            _l.error("Task: executing script, failed! You can find more information in error logs.");
                        }
                    }
                    else if (executableTask.TaskType.Equals(BaseWebDriverTasksTypes.TaskDelayTask))
                    {
                        if (executableTask.DelayTime > 0)
                        {
                            Thread.Sleep(executableTask.DelayTime);
                        }
                    }
                    else if (executableTask.TaskType.Equals(BaseWebDriverTasksTypes.TaskGetDataFromPage))
                    {
                        if (!executableTask.AssignToField.Equals(String.Empty)
                                && !executableTask.ScriptSource.Equals(String.Empty))
                        {
                            string source = String.Empty;
                            switch (executableTask.ScriptSourceType)
                            {
                                case WebScrapper.Scrapper.Entities.enums.By.FileSource:
                                    source = _f.Read($"{Directory.GetCurrentDirectory()}/Scrapper/Resources/{executableTask.ScriptSource}");
                                    if (ReferenceEquals(source, String.Empty))
                                    {
                                        _l.error($"Fatal: cannot load script from file {Directory.GetCurrentDirectory()}/Scrapper/Resources/{executableTask.ScriptSource}");
                                        WebDriver.Dispose();
                                        return;
                                    }
                                    break;
                                default:
                                    source = executableTask.ScriptSource;
                                    break;
                            }
                            if (!ReferenceEquals(executableTask.LoadDriverDependencies, null)
                                && !ReferenceEquals(executableTask.LoadDriverDependencies, false))
                            {
                                if (ReferenceEquals(executableTask.DriverDependencies, null)
                                    || ReferenceEquals(executableTask.DriverDependencies.Length, 0))
                                {
                                    _l.error("Chrome driver: fatal: instance required driver dependencies, but dependencies list doesn't contains any dependencies!");
                                    WebDriver.Dispose();
                                    return;
                                }
                                string _driverTempSources = String.Empty;
                                foreach (var dependency in executableTask.DriverDependencies)
                                {
                                    string _s = _f.Read($"{WebDriver.GetDriverDependenciesDirectory()}{dependency}");
                                    if (ReferenceEquals(_s, String.Empty))
                                    {
                                        _l.error($"Chromedriver: fatal: cannot load dependency {WebDriver.GetDriverDependenciesDirectory()}{dependency} . Are you sure, that this file exists and its connect can be read by our program?");
                                        WebDriver.Dispose();
                                        return;
                                    }
                                    _driverTempSources += String.Concat(" ", _s);
                                }
                                source += _driverTempSources;
                            }
                            var tempData = WebDriver.GetDataFromPage(null, source);
                            if (!ReferenceEquals(tempData, null) && !ReferenceEquals(tempData, String.Empty))
                            {
                                if (SetByReflectMethod(tempData, executableTask.AssignToField))
                                {
                                    _l.info("Task: getting source form page: success");
                                }
                                else
                                {
                                    _l.error("Task: getting source from page: fatal error in reflection section.");
                                }
                            }
                        }
                    }
                    else if (executableTask.TaskType.Equals(BaseWebDriverTasksTypes.TaskAwaitTask))
                    {
                        int CurrentIteration = 0;
                        int IterationsLimit = 20;
                        int ThreadDelay = 500;
                        bool TaskResult = false;
                        if (!ReferenceEquals(executableTask.AwaitParams.AwaitMaxAttempts, null))
                        {
                            IterationsLimit = executableTask.AwaitParams.AwaitMaxAttempts;
                        }
                        if (!ReferenceEquals(executableTask.AwaitParams.AwaitDelayTime, null))
                        {
                            ThreadDelay = executableTask.AwaitParams.AwaitDelayTime;
                        }
                        if (IterationsLimit > 0)
                        {
                            while (CurrentIteration < IterationsLimit)
                            {
                                try
                                {
                                    switch (executableTask.AwaitParams.AwaitCheckingStrategy)
                                    {
                                        case BaseWebDriverAwaitTaskAction.SwitchToFrame:
                                            if (!WebDriver.SwitchToFrame(executableTask.RequestElement))
                                            {
                                                throw new Exception();
                                            }
                                            TaskResult = true;
                                            break;
                                        case BaseWebDriverAwaitTaskAction.SelectElement:
                                            if (!WebDriver.IsElementExists(executableTask.RequestElement))
                                            {
                                                throw new Exception();
                                            }
                                            TaskResult = true;
                                            break;
                                    }
                                }
                                catch (Exception)
                                {
                                    CurrentIteration++;
                                    Thread.Sleep(ThreadDelay);
                                    continue;
                                }
                                if (TaskResult)
                                {
                                    break;
                                }
                            }
                            if (!TaskResult)
                            {
                                _l.error("Driver task: execution failed, see error log to get more information");
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _l.error($"WebDriver task error: {e.Message} -> {e.StackTrace}");
            }
            finally
            {
                WebDriver?.Dispose();
            }
        }
        #endregion

        #region Base scrapper methods 
        protected string ScrapByInnerText(StrategyHtmlEntity strategy)
        {
            if (strategy.SelectionType.Equals(StrategyHtmlSelectionType.StrategyMultipleSelection))
            {
                var collection = BaseScrappMultiple(strategy.BaseItemSelector);
                if (!ReferenceEquals(collection, null))
                {
                    if (!ReferenceEquals(strategy.SelectByIndexFromRange, null))
                    {
                        int rangeIdentifier = 0;
                        int currentPosition = 0;
                        if (strategy.SelectByIndexFromRange.Equals("prelast"))
                        {
                            rangeIdentifier = collection.Count - 2;
                        }
                        else if (strategy.SelectByIndexFromRange.Equals("last"))
                        {
                            rangeIdentifier = collection.Count - 1;
                        }
                        else
                        {
                            rangeIdentifier = int.Parse(strategy.SelectByIndexFromRange) - 1;
                        }
                        foreach (var i in collection)
                        {
                            if (currentPosition < rangeIdentifier)
                            {
                                currentPosition++;
                                continue;
                            }
                            return i.InnerText;
                        }
                    }
                }
                return null;
            }
            else
            {
                return BaseScrapp(strategy.BaseItemSelector)?.InnerText;
            }
        }
        protected string ScrapByInnerHtml(StrategyHtmlEntity strategy)
        {
            if (strategy.SelectionType.Equals(StrategyHtmlSelectionType.StrategyMultipleSelection))
            {
                var collection = BaseScrappMultiple(strategy.BaseItemSelector);
                if (!ReferenceEquals(collection, null))
                {
                    if (!ReferenceEquals(strategy.SelectByIndexFromRange, null))
                    {
                        int rangeIdentifier = 0;
                        int currentPosition = 0;
                        if (strategy.SelectByIndexFromRange.Equals("prelast"))
                        {
                            rangeIdentifier = collection.Count - 2;
                        }
                        else if (strategy.SelectByIndexFromRange.Equals("last"))
                        {
                            rangeIdentifier = collection.Count - 1;
                        }
                        else
                        {
                            rangeIdentifier = int.Parse(strategy.SelectByIndexFromRange) - 1;
                        }
                        foreach (var i in collection)
                        {
                            if (currentPosition < rangeIdentifier)
                            {
                                currentPosition++;
                                continue;
                            }
                            return i.InnerHtml;
                        }
                    }
                }
                return null;
            }
            else
            {
                return BaseScrapp(strategy.BaseItemSelector)?.InnerHtml;
            }
        }
        #endregion 

        #region Scrapper nodes handlers
        protected HtmlNode BaseScrapp(string selector)
        {
            try
            {
                var AbstractElementNode = NodeHandler.SelectSingleElement(selector);
                if (!ReferenceEquals(AbstractElementNode, null))
                {
                    return AbstractElementNode;
                }
                // _l.warn($"Nothing scrapped from node {selector}");
                return null;
            }
            catch (Exception e)
            {
                _l.error($"Iternal scrapping error: {e.Message}. Stack Trace: {e.StackTrace}");
                return null;
            }
        }

        protected List<HtmlNode> BaseScrappMultiple(string selector)
        {
            try
            {
                var AbstractElementNode = NodeHandler.SelectElementsCollection(selector);
                if (!ReferenceEquals(AbstractElementNode, null))
                {
                    return AbstractElementNode;
                }
                // _l.warn($"Nothing scrapped from node {selector}");
                return null;
            }
            catch (Exception e)
            {
                _l.error($"Iternal scrapping error: {e.Message}. Stack Trace: {e.StackTrace}");
                return null;
            }
        }
        #endregion 

        #region Reflection

        public bool SetByReflectMethod(string value, string field)
        {
            try
            {
                var ReflectionFields = AkeneoProduct.GetType().GetFields(BindingFlags);
                if (!ReferenceEquals(ReflectionFields, null))
                {
                    foreach (var ReflectionField in ReflectionFields)
                    {
                        if (!ReferenceEquals(ReflectionField, null))
                        {
                            try
                            {
                                if (ReflectionField.Name.Contains(field))
                                {
                                    var AbstractElementCurrentValue = ReflectionField.FieldType.FullName;
                                    if (AbstractElementCurrentValue.Equals("System.String"))
                                    {
                                        ReflectionField.SetValue(AkeneoProduct, value.ToString());
                                    }
                                    else if (AbstractElementCurrentValue.Equals("System.Int32"))
                                    {
                                        ReflectionField.SetValue(AkeneoProduct, int.Parse(value));
                                    }
                                    else if (AbstractElementCurrentValue.Equals("System.Boolean"))
                                    {
                                        ReflectionField.SetValue(AkeneoProduct, bool.Parse(value));
                                    }
                                    return true;
                                }
                            }
                            catch (Exception e)
                            {
                                _l.error($"System.Reflection error -> {e.Message}. {e.StackTrace}");
                            }
                        }
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                _l.error($"System.Reflection fatal error: {e.Message} -> {e.StackTrace}");
                return false;
            }
        }

        public string GetByReflectMethod(string field)
        {
            try
            {
                var ReflectionFields = AkeneoProduct.GetType().GetFields(BindingFlags);
                if (!ReferenceEquals(ReflectionFields, null))
                {
                    foreach (var ReflectionField in ReflectionFields)
                    {
                        if (!ReferenceEquals(ReflectionField, null))
                        {
                            try
                            {
                                if (ReflectionField.Name.Contains(field))
                                {
                                    var AbstractElementCurrentValue = ReflectionField.GetValue(AkeneoProduct).ToString();
                                    return AbstractElementCurrentValue;
                                }
                            }
                            catch (Exception e)
                            {
                                _l.error($"System.Reflection error -> {e.Message}. {e.StackTrace}");
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                _l.error($"System.Reflection fatal error: {e.Message} -> {e.StackTrace}");
                return null;
            }
        }
        #endregion

        #region String generators
        private string GenerateRandomProductIdentifier()
        {
            var lower = 2;
            var higher = 8;
            var iterations = new Random().Next(2, 6);
            var Random = new Random();
            int i = 0;
            string output = RandomString(Random.Next(lower, higher));
            while (i <= iterations)
            {
                output = $"{output}-{RandomString(Random.Next(lower, higher))}";
                i++;
            }

            return output;
        }

        private string RandomString(int size, bool lowerCase = false)
        {
            var builder = new StringBuilder(size);
            var _random = new Random();

            char offset = lowerCase ? 'a' : 'A';
            const int lettersOffset = 26;

            for (var i = 0; i < size; i++)
            {
                var @char = (char)_random.Next(offset, offset + lettersOffset);
                builder.Append(@char);
            }

            return lowerCase ? builder.ToString().ToLower() : builder.ToString();
        }
        #endregion

        #region Custom scrapping Algorithms
        protected void ScrapByGrassCityJs(StrategyHtmlEntity entity)
        {
            if (ReferenceEquals(entity, null) || ReferenceEquals(entity.BaseItemSelector, null))
            {
                _l.error($"Strategy: StrategyPuffingBirdTags : null refference error: strategy doesn't contains any definitions or selectors");
                return;
            }
            var node = BaseScrapp(entity.BaseItemSelector);
            if (ReferenceEquals(node, null))
            {
                _l.error($"Strategy: StrategyPuffingBirdTags : nothing scrapped from node {entity.BaseItemSelector}");
                return;
            }
            try
            {
                var prepared = Regex.Match(node.InnerHtml, @",{.+}]", RegexOptions.IgnoreCase).Value.Replace("}]", "}").Replace(",{", "{");
                var exploded = prepared.Split(");");
                var schema = JsonConvert.DeserializeObject<BaseScrapperGrassCityJsEntity>(exploded[0]);
                if (ReferenceEquals(schema, null))
                {
                    return;
                }
                if (!ReferenceEquals(schema.ProductSku, null))
                {
                    AkeneoProduct.productCode = schema.ProductSku;
                }
                if (!ReferenceEquals(schema.ProductCategories, null) && schema.ProductCategories.Count() > 0)
                {
                    var temp = new List<string>();
                    var current = 0;
                    var limit = 2;
                    foreach (var cat in schema.ProductCategories)
                    {
                        if (current == 1)
                        {
                            AkeneoProduct.productCategory = cat;
                        }
                        else
                        {
                            temp.Add(cat);
                        }
                        current++;
                        if (current > limit)
                        {
                            break;
                        }
                    }
                    AkeneoProduct.productTags = String.Join(",", temp);
                }
            }
            catch (Exception e)
            {
                _l.error($"Fatal error in processing grasscity js: {e.Message} -> {e.StackTrace}");
            }
        }
        protected void ScrapByProductJsTemplate(StrategyHtmlEntity entity)
        {
            if (ReferenceEquals(entity, null) || ReferenceEquals(entity.BaseItemSelector, null))
            {
                _l.error($"Strategy: StrategyPuffingBirdTags : null refference error: strategy doesn't contains any definitions or selectors");
                return;
            }
            var node = BaseScrapp(entity.BaseItemSelector);
            if (ReferenceEquals(node, null))
            {
                _l.error($"Strategy: StrategyPuffingBirdTags : nothing scrapped from node {entity.BaseItemSelector}");
                return;
            }
            try
            {
                var productTemplate = JsonConvert.DeserializeObject<ShopifyJsonProductTemplate>(node.InnerText);
                if (!ReferenceEquals(productTemplate.ProductTags, null) && productTemplate.ProductTags.Count > 0)
                {
                    int limit = 3;
                    int current = 0;
                    var temp = new List<string>();
                    foreach (var cat in productTemplate.ProductTags)
                    {
                        if (limit <= current)
                        {
                            break;
                        }
                        temp.Add(cat);
                        current++;
                    }
                    productTemplate.PreparedTags = String.Join(",", temp);
                }
                var describedSchema = productTemplate.GetType().GetFields(BindingFlags);
                foreach (var field in describedSchema)
                {
                    foreach (var attr in entity.AttributesList)
                    {
                        if (field.Name.Contains(attr.AttributeName))
                        {
                            if (SetByReflectMethod(field.GetValue(productTemplate).ToString(), attr.AttributeAssingToRule))
                            {
                                _l.info($"Schema successfully processed!");
                            }
                            else
                            {
                                _l.error($"Error in schema processing, you can find more information in application error logs");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _l.error($"(Exception): Cannot deconvert product schema json string {e.Message}. Stack trace: {e.StackTrace}");
                return;
            }
        }
        protected void ScrapByShopifyDeliveryJs(StrategyHtmlEntity entity)
        {
            if (ReferenceEquals(entity, null) || ReferenceEquals(entity.BaseItemSelector, null))
            {
                _l.error($"Strategy: StrategyPuffingBirdTags : null refference error: strategy doesn't contains any definitions or selectors");
                return;
            }
            var node = BaseScrapp(entity.BaseItemSelector);
            if (ReferenceEquals(node, null))
            {
                _l.error($"Strategy: StrategyPuffingBirdTags : nothing scrapped from node {entity.BaseItemSelector}");
                return;
            }
            try
            {
                var preparedScript = node.InnerHtml.Trim().Replace("\n", "").Replace("  ", "");
                preparedScript = Regex.Match(preparedScript, @"return {.+}.+}", RegexOptions.IgnoreCase).Value;
                preparedScript = preparedScript.Replace("return ", "").Replace("variants: variants,", "").Replace("})();data;}", "");
                var schema = JsonConvert.DeserializeObject<WebScrapperBaseTagsEntityDto>(preparedScript.Replace("return ", "").Replace("variants: variants,", "").Replace("})();data;}", ""));
                if (!ReferenceEquals(schema.collections, null) && schema.collections.Count > 0 && !schema.collections.First().Equals(String.Empty))
                {
                    schema.firstCategory = Regex.Replace(schema.collections.First(), @"(^\w)|(\s\w)", m => m.Value.ToUpper());

                    int current = 0;
                    int limit = 3;
                    var t = new List<string>();
                    foreach (var collection in schema.collections)
                    {
                        if (current.Equals(0))
                        {
                            current++;
                            continue;
                        }
                        if (current <= limit)
                        {
                            t.Add(Regex.Replace(collection, @"(^\w)|(\s\w)", m => m.Value.ToUpper()));
                        }
                    }
                    schema.productTags = String.Join(",", t);
                    var describedSchema = schema.GetType().GetFields(BindingFlags);
                    foreach (var attr in entity.AttributesList)
                    {
                        foreach (var field in describedSchema)
                        {
                            if (field.Name.Contains(attr.AttributeName))
                            {
                                if (SetByReflectMethod(field.GetValue(schema).ToString(), attr.AttributeAssingToRule))
                                {
                                    _l.info($"Schema successfully processed!");
                                }
                                else
                                {
                                    _l.error($"Error in schema processing, you can find more information in application error logs");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _l.error($"(Exception): Cannot deconvert product schema json string {e.Message}. Stack trace: {e.StackTrace}");
                return;
            }
        }

        protected void ScrapByGraphTemplate(StrategyHtmlEntity entity)
        {
            if (ReferenceEquals(entity, null) || ReferenceEquals(entity.BaseItemSelector, null))
            {
                _l.warn("Graph template: error in scrapping strategy. Empty selector or scrapping object.");
                return;
            }
            if (ReferenceEquals(entity.AttributesList, null) || ReferenceEquals(entity.AttributesList.Count, 0))
            {
                _l.warn("Graph template: error in scrapping strategy. Empty attributes list.");
                return;
            }
            var node = BaseScrapp(entity.BaseItemSelector);
            if (ReferenceEquals(node, null))
            {
                _l.warn($"Graph strategy: nothing scrapped from node {entity.BaseItemSelector}");
                return;
            }
            try
            {
                var schema = JsonConvert.DeserializeObject<WebScrapperBaseGraphEntity>(node.InnerHtml);
                if (!ReferenceEquals(schema, null)
                    && !ReferenceEquals(schema.Graph, null)
                    && schema.Graph.Count > 0)
                {
                    var graph = schema.Graph.FirstOrDefault();
                    if (!ReferenceEquals(graph.Offers.Price, null))
                    {
                        graph.Price = graph.Offers.Price;
                    }
                    else
                    {
                        graph.Price = graph.Offers.LowPrice;
                    }
                    if (!ReferenceEquals(graph.Offers, null)
                        && !ReferenceEquals(graph.Offers.Availability, null))
                    {
                        graph.IsProductInStock = graph.Offers.Availability.Contains("InStock");
                    }
                    else
                    {
                        graph.IsProductInStock = false;
                    }

                    var describedSchema = graph.GetType().GetFields(BindingFlags);
                    foreach (var attr in entity.AttributesList)
                    {
                        foreach (var field in describedSchema)
                        {
                            if (field.Name.Contains(attr.AttributeName))
                            {
                                if (SetByReflectMethod(field.GetValue(graph).ToString(), attr.AttributeAssingToRule))
                                {
                                    _l.info($"Value {field.GetValue(graph).ToString()} for field {attr.AttributeAssingToRule} successfully injected!");
                                }
                                else
                                {
                                    _l.error($"Some errors during injecting field {attr.AttributeAssingToRule} occured, you can find more information in application error logs");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _l.error($"Graph template scrapper: fatal error: {e.Message} -> {e.StackTrace}");
                return;
            }
        }
        #endregion
    }
}