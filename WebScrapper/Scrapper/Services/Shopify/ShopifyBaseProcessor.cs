using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net;

using WebApplication.Scrapper.Implementation;
using WebApplication.Scrapper.Services.Akeneo.Implementation;

using WebScrapper.Scrapper.Services.Shopify.Enums;
using WebScrapper.Scrapper.Services.Shopify.Entities;
using WebScrapper.Scrapper.Services.Shopify.Abstraction;
using WebScrapper.Scrapper.Services.Shopify.Delegates;
using WebScrapper.Scrapper.Services.Shopify.Entities.Converters;
using WebScrapper.Scrapper.Services.Shopify.Entities.Dto;
using WebScrapper.Scrapper.Entities.enums;
using WebScrapper.Scrapper.Delegates;
using WebScrapper.Scrapper.Entities;
using Newtonsoft.Json;

namespace WebScrapper.Scrapper.Services.Shopify
{
    public class ShopifyBaseProcessor : IBaseShopifyProcessor
    {
        public string ServiceName { get; set; }
        private ShopifyBaseProcessorSettings Settings { get; set; }
        private LogsWriter _l { get; set; }

        private BaseServicesStatuses ListingStatus { get; set; }
        private List<BaseShopifyProductEntity> ShopifyProductsList { get; set; }
        public ShopifyBaseProcessor(ShopifyBaseProcessorSettings _settings, LogsWriter logger)
        {
            ServiceName = "ShopifyService";
            Settings = _settings;
            _l = logger;
            ListingStatus = BaseServicesStatuses.ServiceNotLaunched;
            ShopifyProductsList = new List<BaseShopifyProductEntity>();
        }

        public event BaseServiceCallBack OnShopifyIndexationFinished;

        public void ListProducts()
        {
            if (ListingStatus.Equals(BaseServicesStatuses.ServiceNotLaunched))
            {
                ListingStatus = BaseServicesStatuses.ServiceLaunching;
                new Thread(() => {
                    ProductsListenerThread($"{Settings.ShopifyApiProtocol}{Settings.ShopifyStoreUrl}{Settings.ShopifyBaseProuctsListUrl}?limit=250");
                }).Start();
            }
        }

        public void ProductsListenerThread(string requestUrl)
        {
            var httpManager = new BaseWebClientWriter();
            try
            {
                var credentials = new NetworkCredential(Settings.ShopifyApiKey, Settings.ShopifyApiToken);
                var ShopifyResponse = httpManager.GetData(requestUrl, credentials);
                var lastHeader = httpManager.LastHeader;
                if (lastHeader.Contains("next"))
                {
                    string next = lastHeader.Replace("<", "").Replace(">; rel=\"next\"", "");
                    new Thread(() => {
                        ProductsListenerThread(next);
                    }).Start();
                }
                if (!ShopifyResponse.Equals(String.Empty))
                {
                    var productsList = JsonConvert.DeserializeObject<ShopifyBaseDtoObject>(ShopifyResponse);
                    if (!ReferenceEquals(productsList.ProductsCollection, null) && productsList.ProductsCollection.Count > 0)
                    {
                        foreach (var product in productsList.ProductsCollection)
                        {
                            ShopifyEntitesBaseConverter _c = new ShopifyEntitesBaseConverter();
                            ShopifyProductsList.Add(_c.ConvertToApplicationEntity(product));
                        }
                    }
                }
                if (!lastHeader.Contains("next"))
                {
                    InvokeOnShopifyIndexationFinished();
                }
            } catch (Exception e)
            {
                _l.error($"Error during sending request to {requestUrl} -> {e.Message} : {e.StackTrace}");
            }
        }

        public void InvokeOnShopifyIndexationFinished()
        {
            ListingStatus = BaseServicesStatuses.ServiceLaunched;
            OnShopifyIndexationFinished?.Invoke(this, new BaseServiceResponse());
        }

        public bool UpdateProduct(string ProductName)
        {
            _l.info($"Shopify service: starting updating procedure for product {ProductName}.");
            _l.info($"Shopify service: searching product {ProductName} in shopify list.");
            var product = FindProductByName(ProductName);
            if (!ReferenceEquals(ProductName, null))
            {
                _l.info($"Shopify service: making product {ProductName} out of stock in shopify.");
                return MakeProductOutOfStock(product);
            }
            _l.info($"Shopify service: cancel product {product.ProductName} doesn't exists in the shopify");
            return false;
        }

        protected BaseShopifyProductEntity FindProductByName(string ProductName)
        {
            if (ReferenceEquals(ProductName, null) 
                || ProductName.Equals(String.Empty))
            {
                return null;
            }
            try
            {
                var selected = ShopifyProductsList.Where(e => e.ProductName.Equals(ProductName)).ToList();
                if (!ReferenceEquals(selected, null))
                {
                    return selected.First();
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        protected bool MakeProductOutOfStock(BaseShopifyProductEntity product) 
        {
            _l.info($"Shopify service -> WebClient: start.");
            try
            {
                _l.info($"Shopify service -> WebClient: started with success code.");
                var httpManager = new BaseWebClientWriter();
                _l.info($"Shopify service -> WebClient: creating auth credentials");
                var credentials = new NetworkCredential(Settings.ShopifyApiKey, Settings.ShopifyApiToken);
                _l.info($"Shopify service -> WebClient: auth credentials successfully created");
                _l.info($"Shopify service -> WebClient: sending request to {Settings.ShopifyApiProtocol}{Settings.ShopifyStoreUrl}{Settings.ShopifyBaseProuctsUpdateUrl}{product.ProductId}{Settings.ShopifyBaseProuctsUpdateUrlExtension}");
                string productEncoded = httpManager.GetData($"{Settings.ShopifyApiProtocol}{Settings.ShopifyStoreUrl}{Settings.ShopifyBaseProuctsUpdateUrl}{product.ProductId}{Settings.ShopifyBaseProuctsUpdateUrlExtension}", credentials);
                _l.info("Shopify service -> request finished, checking results...");
                if (!ReferenceEquals(productEncoded, null) && !productEncoded.Equals(String.Empty))
                {
                    _l.info("Shopify service: decoding json string to application dto");
                    ShopifyBaseDtoObject productS = JsonConvert.DeserializeObject<ShopifyBaseDtoObject>(productEncoded);
                    _l.info("Shopify service: json string successfully decoded to application dto");
                    _l.info($"Shopify service: creating body for request.");
                    string body = String.Concat("{\"product\":{\"id\":", product.ProductId, ", \"variants\":[{", $"\"id\":{productS.Product.ProductVariants.FirstOrDefault().VariantIdentifier}, \"inventory_policy\": \"deny\", \"inventory_quantity\":0, \"inventory_management\": \"shopify\"", "}]}}");
                    _l.info($"Shopify service: body request successfuly created -> {body}");
                    _l.info($"Shopify service -> WebClient: adding string body context to the request.");
                    httpManager.AddBodyParameter(body);
                    _l.info($"Shopify service -> WebClient: body context successfully added to the request.");
                    _l.info($"Shopify service -> WebClient: sending request to the shopify");
                    var response = httpManager.PutData($"{Settings.ShopifyApiProtocol}{Settings.ShopifyStoreUrl}{Settings.ShopifyBaseProuctsUpdateUrl}{product.ProductId}{Settings.ShopifyBaseProuctsUpdateUrlExtension}", credentials);
                    _l.info($"Shopify service -> WebClient: request successfully processed with success code!");
                    return true;
                }
                else 
                {
                    _l.error($"Shopify service: fatal, cannot vaidate response from {Settings.ShopifyApiProtocol}{Settings.ShopifyStoreUrl}{Settings.ShopifyBaseProuctsUpdateUrl}{product.ProductId}{Settings.ShopifyBaseProuctsUpdateUrlExtension}");
                    return false;
                }
            }
            catch (Exception e)
            {
                _l.error($"Shopify service: fatal, {e.Message} -> {e.StackTrace}");
                return false;
            }
        }

        public BaseServicesStatuses GetServiceStatus()
        {
            return !ReferenceEquals(ListingStatus, null) ? ListingStatus : BaseServicesStatuses.ServiceError;
        }

    }
}
