using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;

using Newtonsoft.Json;

using WebApplication.Scrapper.Entities;
using WebApplication.Scrapper.Implementation;
using WebApplication.Scrapper.Services.Akeneo.Abstraction;
using WebApplication.Scrapper.Services.Akeneo.Entities;
using WebApplication.Scrapper.Services.Akeneo.Enums;
using WebApplication.Scrapper.Services.Akeneo.Implementation;
using WebApplication.Scrapper.Services.Akeneo.Converters;
using WebApplication.Scrapper.Services.Akeneo.Delegates;
using WebApplication.Scrapper.Services.Akeneo.Services;
using WebApplication.Scrapper.Services.Akeneo.Entities.Dto.IndexedCategoriesDto;

using WebScrapper.Scrapper.Entities.enums;
using WebScrapper.Scrapper.Delegates;
using WebScrapper.Scrapper.Entities;
using WebScrapper.Scrapper.Services.Akeneo.Entities;

namespace WebApplication.Scrapper.Services.Akeneo
{
    public class AkeneoBaseWriter : IBasePimWriter<AkeneoProduct>
    {
        public string ServiceName { get; set; }
        private Timer AkeneoUpdaterTimer;
        private string akeneoBaseUrl { get; set; }
        //client:secret
        private string akeneoBaseAuthInfo { get; set; }
        private BaseServicesStatuses akeneoStatus { get; set; }
        private AkeneoAuthEntity akeneoAuthInfo { get; set; }
        private AkeneoBaseInformation Akeneo { get; set; }
        private IBaseRequestHandler<NameValueCollection , WebClientHeader> httpManager { get; set; }
        private IBaseListener<AkeneoProduct, AkeneoBaseInformation> ProductIndexer { get; set; }
        private IBaseListener<AkeneoCategory, AkeneoBaseInformation> CategoriesListener { get; set; }
        public event BaseServiceCallBack OnProductListeningFinished;
        private List<AkeneoProduct> ListedProductsList { get; set; }
        private List<AkeneoCategory> ListedCategoriesList { get; set; }
        private List<CollectionsAsignerEntity> CollectionRules { get; set; }
        private List<VendorsAssignerEntity> VendorsRules { get; set; }
        private LogsWriter _logger;
        public AkeneoBaseWriter(LogsWriter logger, List<CollectionsAsignerEntity> collectionRules, List<VendorsAssignerEntity> vendorsRules)
        {
            ServiceName = "AkeneoService";
            akeneoStatus = BaseServicesStatuses.ServiceNotLaunched;
            _logger = logger;
            
            httpManager = new BaseWebClientWriter();
            
            Akeneo = new AkeneoBaseInformation();
            Akeneo.BaseAkeneoUrl = "http://localhost:8080/";
            Akeneo.BaseAkeneoUserName = "sharp_7630";
            Akeneo.BaseAkeneoPassword = "c6f5484d8";
            Akeneo.BaseAkeneoClientId = "1_3001g7f521uso44g0ggk8wk04c0ws0co8cc0ok0g08skg8k4k0";
            Akeneo.BaseAkeneoSecretKey = "4yde7syn96w448cockwwoo0oso00cskckkkwk888w40888gcg8";
            
            Connect(new object());
            
            CategoriesListener = new AkeneoBaseCategoriesListener(Akeneo, akeneoAuthInfo.access_token, logger);
            CategoriesListener.OnFinishedListing += AkeneoCategoriesListingCallback;

            ProductIndexer = new AkeneoBaseProductIndexer(Akeneo, akeneoAuthInfo.access_token, logger);
            ProductIndexer.OnFinishedListing += AkeneoListingReadyCallback;

            if (!ReferenceEquals(collectionRules, null))
            {
                CollectionRules = collectionRules;
            }
            else 
            {
                collectionRules = new List<CollectionsAsignerEntity>();
                _logger.warn("Akeneo: Empty collections rules received.");
            }

            if (!ReferenceEquals(vendorsRules, null))
            {
                VendorsRules = vendorsRules;
            }
            else
            {
                vendorsRules = new List<VendorsAssignerEntity>();
                _logger.warn("Akeneo: Empty vendors rules received");
            }
        }

        public void LaunchInstance()
        {
            ProductIndexer.List();
            CategoriesListener.List();
        }

        public void AkeneoCategoriesListingCallback(object sender, PimProductsListenerCallbackResult eventArgs) 
        {
            if (!ReferenceEquals(CategoriesListener.RequestList, null) && CategoriesListener.RequestList.Count > 0)
            {
                ListedCategoriesList = CategoriesListener.RequestList;
            }
            else
            {
                ListedCategoriesList = new List<AkeneoCategory>();
            }
            CategoriesListener.OnFinishedListing -= AkeneoCategoriesListingCallback;
            if (!ReferenceEquals(ListedProductsList, null))
            {
                InvokeOnProductListeningFinished();
            }
        }
        public void AkeneoListingReadyCallback(object sender, PimProductsListenerCallbackResult eventArgs)
        {
            if (eventArgs.indexedProductsList != null && eventArgs.indexedProductsList.Count > 0)
            {
                ListedProductsList = eventArgs.indexedProductsList;
            }
            else
            {
                ListedProductsList = new List<AkeneoProduct>();
            }

            ProductIndexer.OnFinishedListing -= AkeneoListingReadyCallback;
            if (!ReferenceEquals(ListedCategoriesList, null))
            {
                InvokeOnProductListeningFinished();
            }
        }

        public void InvokeOnProductListeningFinished()
        {
            OnProductListeningFinished?.Invoke(this, new BaseServiceResponse());
        }
        public AkeneoProduct Preprocess(AkeneoProduct product)
        {
            if (!ReferenceEquals(CollectionRules, null) & CollectionRules.Count > 0)
            {
                try
                {
                    var targetCollection = CollectionRules.Where(c => c.CollectionToMatch.Trim().Equals(product.productCategory.Trim())).ToList();
                    if (!ReferenceEquals(targetCollection, null) && targetCollection.Count > 0)
                    {
                        product.productCategory = targetCollection.FirstOrDefault().CollectionToAssign;
                    }
                }
                catch (Exception)
                {
                    
                }
            }
            if (!ReferenceEquals(VendorsRules, null) && VendorsRules.Count > 0)
            {
                try
                {
                    var targetVendor = VendorsRules.Where(v => v.VendorToCompare.Trim().Equals(product.productVendor.Trim())).ToList();
                    if (!ReferenceEquals(targetVendor, null) && targetVendor.Count > 0)
                    {
                        product.productVendor = targetVendor.FirstOrDefault().VendorToAssign;
                    }
                }
                catch (Exception)
                {
                    
                }
            }
            if (ReferenceEquals(product.productCategory, String.Empty) || ReferenceEquals(product.productCategory, null)) 
            {
                product.productCategory = "No idea";
            }
            var _tempCat = GetCategoryId(product.productCategory);
            if (_tempCat.Equals(String.Empty))
            {
                if (ReferenceEquals(product.productCategory, null))
                {
                    product.productCategory = String.Empty;
                }
                AkeneoCategory _cat = new AkeneoCategory();
                if (product.productCategory.Equals(String.Empty))
                {
                    _cat.CategoryName = "Empty scrapping node";
                }
                else
                {
                    _cat.CategoryName = product.productCategory;
                }
                _cat = CreateNewCategory(_cat, product);
                if (!ReferenceEquals(_cat, null))
                {
                    lock (ListedCategoriesList)
                    {
                        ListedCategoriesList.Add(_cat);
                    }
                    product.productCategory = _cat.CategoryId;
                }
            }
            else 
            {
                product.productCategory = _tempCat;
            }
            return product;
        }
        public bool ProcessProduct(AkeneoProduct product)
        {
            if (akeneoStatus.Equals(BaseServicesStatuses.ServiceLaunching))
            {
                int connectionLimit = 10;
                int currentConnection = 0;
                while (currentConnection < connectionLimit)
                {
                    _logger.info("Akeneo is connectng, waiting for its response");
                    Thread.Sleep(1000);
                    currentConnection++;
                    if (akeneoStatus.Equals(BaseServicesStatuses.ServiceLaunched))
                    {
                        break;
                    }
                }
                if (!akeneoStatus.Equals(BaseServicesStatuses.ServiceLaunched))
                {
                    throw new Exception("Fatal error: Akeneo is not connected, check its settings");
                }
                if (akeneoStatus.Equals(BaseServicesStatuses.ServiceLaunched))
                {
                    _logger.info("Akeneo successfully reconnected!");
                }
            }
            if (product.productId.Equals(String.Empty))
            {
                throw new Exception($"(AkeneoProductEntitiesException) -> cannot verify identifier for product {product.productName} with url {product.productUrl}");
            }
            product = Preprocess(product);
            if (IsProductExists(product.productName))
            {
                var existsProduct = ListedProductsList.Where(e => e.productCode.Equals(product.productCode)).ToList();
                if (existsProduct.FirstOrDefault() != null)
                {
                    product.productId = existsProduct.FirstOrDefault().productId;
                }

                _logger.info($"Updating product {product.productName}");
                return UpdateExistsProduct(product);
            }
            
            _logger.info($"Creating product {product.productName}");
            return CreateNewProduct(product);
        }

        public string GetCategoryId(string category)
        {
            if (ReferenceEquals(ListedCategoriesList, null) || ListedCategoriesList.Count.Equals(0))
            {
                return String.Empty;
            }
            try
            {
                var _r = ListedCategoriesList.FirstOrDefault(c => c.CategoryName.Trim().ToLower().Equals(category.Trim().ToLower()));
                if(!ReferenceEquals(_r, null))
                {
                    return _r.CategoryId;
                }
                return String.Empty;
            }
            catch (Exception)
            {
                return String.Empty;
            }
        }

        public bool IsProductExists(string identifier)
        {
            if (ListedProductsList == null || ListedProductsList.Count == 0)
            {
                return false;
            }

            try
            {
                var result = ListedProductsList.Where(e => e.productName.Equals(identifier)).ToList();
                return true;
            }
            catch (NullReferenceException)
            {
                return false;
            }
        }

        public AkeneoCategory CreateNewCategory(AkeneoCategory category, AkeneoProduct product)
        {
            try
            {
                _logger.info($"Creating category {category.CategoryName} : {product.productName} -> start");
                IBaseRequestHandler<NameValueCollection , WebClientHeader> httpManager = new BaseWebClientWriter();
                var _c = new AkeneoBaseCategoriesIndexerConverter();
                category.CategoryId = category.CategoryName.Trim().ToLower().Replace(" ", "_").Replace("-", "_").Replace("/", "_").Replace("?", "_").Replace(".", "_").Replace(",", "_").Replace("&", "_");
                AkeneoIndexedCategoriesDtoEmbedCollectionItem dtoCat = _c.ConvertToDtoEntity(category);
                if (!ReferenceEquals(dtoCat, null) 
                    && !ReferenceEquals(dtoCat.CategoryCode, null)
                    && !ReferenceEquals(dtoCat.CategoriesLocales, null) 
                    && !ReferenceEquals(dtoCat.CategoriesLocales.Count, 0))
                {
                    var EncodedContent = JsonConvert.SerializeObject(dtoCat);                
                    httpManager.AddHeader(new WebClientHeader("Authorization", $"Bearer {akeneoAuthInfo.access_token}"));
                    httpManager.AddBodyParameter(EncodedContent);
                    httpManager.PostDataAsStringContext($"{Akeneo.BaseAkeneoUrl}{Akeneo.AkeneoCategoryListUrl}");
                    return category;
                }
                return null;
                //AkeneoIndexedCategoriesDtoEmbedCollectionItem
            }
            catch (Exception e)
            {
                _logger.error($"{category.CategoryName} -> {product.productName} Error in creating akeneo category: {e.Message} -> {e.StackTrace}");
                return null;
            }
        }

        public bool CreateNewProduct(AkeneoProduct product)
        {
            if (akeneoStatus.Equals(BaseServicesStatuses.ServiceLaunched))
            {
                return BasePost(product, $"{Akeneo.BaseAkeneoUrl}{Akeneo.AkeneoProductCreateUrl}");
            }
            else
            {
                _logger.error($"Error during connecting to the Akeneo, current status is {akeneoStatus}");
                return false;
            }
        }

        public bool UpdateExistsProduct(AkeneoProduct product)
        {
            if (akeneoStatus.Equals(BaseServicesStatuses.ServiceLaunched))
            {
                return BasePatch(product, $"{Akeneo.BaseAkeneoUrl}{Akeneo.AkeneoProductCreateUrl}/{product.productId}");
            }
            else
            {
                _logger.error($"(Updating): Error during connecting to the Akeneo, current status is {akeneoStatus}");
                return false;
            }
        }

        public bool BasePost(AkeneoProduct product, string requestUrl)
        {
            return BaseSend(product, requestUrl, "POST");
        }

        public bool BasePatch(AkeneoProduct product, string requestUrl)
        {
            return BaseSend(product, requestUrl, "PATCH");
        }

        public bool BaseSend(AkeneoProduct product, string requestUrl, string protocol)
        {
            IBaseRequestHandler<NameValueCollection , WebClientHeader> httpManager = new BaseWebClientWriter();
            var EntitiesConverter = new AkeneoBaseProductEntityConverter();
            var AkeneoProductDto = EntitiesConverter.ConvertToDtoEntity(product);
            try
            {
                var EncodedContent = JsonConvert.SerializeObject(AkeneoProductDto);               
                httpManager.AddHeader(new WebClientHeader("Authorization", $"Bearer {akeneoAuthInfo.access_token}"));
                httpManager.AddBodyParameter(EncodedContent);
                var akeneoBaseResponse = String.Empty;
                try
                {
                    switch (protocol)
                    {
                        case "POST" :
                            akeneoBaseResponse = httpManager.PostDataAsStringContext($"{Akeneo.BaseAkeneoUrl}{Akeneo.AkeneoProductCreateUrl}");
                            break;
                        case "PATCH" :
                            akeneoBaseResponse = httpManager.PatchData(requestUrl);
                            break;
                    }
                    
                    _logger.info($"(Creating) Product {product.productName} successfully created! Akeneo response is: {akeneoBaseResponse}");
                }
                catch (Exception e)
                {
                    _logger.error($"(Creating) Some error detected during creating product ({product.productName}), Akeneo response is: {e.Message}. ({product.productName})");
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                _logger.error($"(Creating): (Exception): error detected, during preparing information for product {product.productName} -> {e.Message}.");
                return false;
            }
        }

        public bool BaseGet(AkeneoProduct product, string requestUrl)
        {
            try
            {
                IBaseRequestHandler<NameValueCollection, WebClientHeader> httpManager = new BaseWebClientWriter();
                httpManager.GetData(requestUrl, new WebClientHeader("", ""));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void Connect(object obj)
        {
            _logger.info("Akeneo -> connecting");
            IBaseRequestHandler<NameValueCollection , WebClientHeader> httpManager = new BaseWebClientWriter();
            akeneoStatus = BaseServicesStatuses.ServiceLaunching;
            var bytes = System.Text.Encoding.UTF8.GetBytes($"{Akeneo.BaseAkeneoClientId}:{Akeneo.BaseAkeneoSecretKey}");
            var auth = System.Convert.ToBase64String(bytes).ToString();
            
            var auth_data = new NameValueCollection();
            
            auth_data.Add("grant_type", Akeneo.AkeneoPasswordGrantType);
            auth_data.Add("username", Akeneo.BaseAkeneoUserName);
            auth_data.Add("password", Akeneo.BaseAkeneoPassword);
            
            httpManager.AddBodyParameter(auth_data);
            httpManager.AddHeader(new WebClientHeader("Authorization", $"Basic {auth}"));
            
            var akeneoBaseResponse = String.Empty;
            try
            {
                akeneoBaseResponse = httpManager.PostData($"{Akeneo.BaseAkeneoUrl}{Akeneo.AkeneoAuthUrl}");
            }
            catch (Exception e)
            {
                akeneoStatus = BaseServicesStatuses.ServiceError;
                _logger.error(e.Message);
                return;
            }

            try
            {
                var akeneoJson =
                    JsonConvert.DeserializeObject<AkeneoAuthEntity>(akeneoBaseResponse);
                if (akeneoJson.access_token.Equals(String.Empty) || akeneoJson.access_token == null)
                {
                    akeneoStatus = BaseServicesStatuses.ServiceError;
                    _logger.error("Cannot obtain new token from Akeneo's API!");
                }
                else
                {
                    akeneoStatus = BaseServicesStatuses.ServiceLaunched;
                    var timer = new TimerCallback(Connect);
                    AkeneoUpdaterTimer = new Timer(timer, null, akeneoJson.expires_in * 1000 - 1, akeneoJson.expires_in * 1000);
                    akeneoAuthInfo = akeneoJson;
                }
            }
            catch (Exception e)
            {
                akeneoStatus = BaseServicesStatuses.ServiceError;
                _logger.error(e.Message);
            }
        }

        public BaseServicesStatuses GetServiceStatus()
        {
            return !ReferenceEquals(akeneoStatus, null) ? akeneoStatus : BaseServicesStatuses.ServiceError;
        }
    }
}