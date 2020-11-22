using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using Newtonsoft.Json;
using WebApplication.Scrapper.Abstraction;
using WebApplication.Scrapper.Entities;
using WebApplication.Scrapper.Services.Akeneo.Abstraction;
using WebApplication.Scrapper.Services.Akeneo.Converters;
using WebApplication.Scrapper.Services.Akeneo.Delegates;
using WebApplication.Scrapper.Services.Akeneo.Entities;
using WebApplication.Scrapper.Services.Akeneo.Entities.Dto.IndexedCategoriesDto;
using WebApplication.Scrapper.Services.Akeneo.Enums;
using WebApplication.Scrapper.Services.Akeneo.Implementation;

using WebScrapper.Scrapper.Services.Akeneo.Entities;

namespace WebApplication.Scrapper.Services.Akeneo.Services
{
    public class AkeneoBaseCategoriesListener : IBaseListener<AkeneoCategory, AkeneoBaseInformation>
    {
        public List<AkeneoCategory> RequestList { get; set; }
        public AkeneoBaseInformation Settings { get; }
        private IBaseRequestHandler<NameValueCollection , WebClientHeader> HttpManager { get; set; }
        private BaseLogger _l;
        public AkeneoProductIndexerStatuses ProcessStatus { get; set; }
        private string AuthToken { get; set; }
        public AkeneoBaseCategoriesListener(AkeneoBaseInformation settings, string token, BaseLogger logger)
        {
            Settings = settings;
            AuthToken = token;
            _l = logger;
            RequestList = new List<AkeneoCategory>();
        }

        public bool IsIndexationFinished()
        {
            return ProcessStatus.Equals(AkeneoProductIndexerStatuses.ListingFinished);
        }

        public void List()
        {
            if (ProcessStatus.Equals(AkeneoProductIndexerStatuses.ListingError))
            {
                _l.error("Fatal error during indexing Akeneo categories, you can find more information in application error logs");
                return;
            }
            if (ProcessStatus.Equals(AkeneoProductIndexerStatuses.ListingStarting) 
                || ProcessStatus.Equals(AkeneoProductIndexerStatuses.ListingInProgress)
                || ProcessStatus.Equals(AkeneoProductIndexerStatuses.ListingFinished))
            {
                _l.warn("Cancel - process already started");
            } 
            else
            {
                ProcessStatus = AkeneoProductIndexerStatuses.ListingStarting;
                new Thread(() => {
                    ListenerThread($"{Settings.BaseAkeneoUrl}{Settings.AkeneoCategoryListUrl}?limit=100");
                }).Start();
            }
        }

        public void ListenerThread(string requestUrl)
        {
            ProcessStatus = AkeneoProductIndexerStatuses.ListingInProgress;
            HttpManager = new BaseWebClientWriter();
            try
            {
                var data = HttpManager.GetData(requestUrl, new WebClientHeader("Authorization", $"Bearer {AuthToken}"));
                AkeneoIndexedCategoriesDto dto = JsonConvert.DeserializeObject<AkeneoIndexedCategoriesDto>(data);
                if (!ReferenceEquals(dto.LinksCollection.NextLink, null))
                {
                    new Thread(() => {
                        ListenerThread(dto.LinksCollection.NextLink.Href);
                    }).Start();
                } 
                if (dto.Embed.ItemsCollection.Count > 0)
                {
                    foreach (var selected in dto.Embed.ItemsCollection)
                    {
                        var _c = new AkeneoBaseCategoriesIndexerConverter();
                        try
                        {
                            var _t = _c.ConvertToApplicationEntity(selected);
                            lock(RequestList)
                            {
                                RequestList.Add(_t);
                            }
                        }
                        catch (Exception e)
                        {
                            _l.error($"AkeneoBaseCategoriesListener exception: cannot convert dto entity to application entity: {e.Message} -> {e.StackTrace}");
                        }
                    }
                }
                if (ReferenceEquals(dto.LinksCollection.NextLink, null))
                {
                    ProcessStatus = AkeneoProductIndexerStatuses.ListingFinished;
                    InvokeOnFinishedListing();
                }
            }
            catch(Exception e)
            {
                _l.error($"Akeneo categories indexer: error occured - {e.Message} -> {e.StackTrace}");
            }
        }

        private void InvokeOnFinishedListing()
        {
            OnFinishedListing?.Invoke(this, new PimProductsListenerCallbackResult(new List<AkeneoProduct>()));
        }

         public event PimProductsListenerCallback OnFinishedListing;
    } 
}