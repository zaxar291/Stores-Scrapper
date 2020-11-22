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
using WebApplication.Scrapper.Services.Akeneo.Entities.Dto.IndexersDto;
using WebApplication.Scrapper.Services.Akeneo.Enums;
using WebApplication.Scrapper.Services.Akeneo.Implementation;

namespace WebApplication.Scrapper.Services.Akeneo.Services
{
    public class AkeneoBaseProductIndexer : IBaseListener<AkeneoProduct, AkeneoBaseInformation>
    {
        public List<AkeneoProduct> RequestList { get; set; }
        public AkeneoBaseInformation Settings { get; }
        private string AuthToken { get; }
        private IBaseRequestHandler<NameValueCollection , WebClientHeader> HttpManager { get; set; }
        private BaseLogger _logger;
        public int ProcessStatus { get; set; }
        
        public AkeneoBaseProductIndexer(AkeneoBaseInformation settings, string token, BaseLogger logger)
        {
            Settings = settings;
            AuthToken = token;
            _logger = logger;
            RequestList = new List<AkeneoProduct>();
            if (ProcessStatus.Equals(0))
            {
                ProcessStatus = (int) AkeneoProductIndexerStatuses.ListingNotStarted;
            }
        }
        
        public bool IsIndexationFinished()
        {
            return ProcessStatus.Equals(AkeneoProductIndexerStatuses.ListingFinished);
        }

        public void List()
        {
            if (ProcessStatus.Equals(AkeneoProductIndexerStatuses.ListingError))
            {
                throw new Exception("Fatal error during indexing Akeneo products, you can find more information in application error logs");
            }

            if (ProcessStatus.Equals(AkeneoProductIndexerStatuses.ListingStarting) 
                || ProcessStatus.Equals(AkeneoProductIndexerStatuses.ListingInProgress)
                || ProcessStatus.Equals(AkeneoProductIndexerStatuses.ListingFinished))
            {
                _logger.warn("Cancel - process already started");
            } 
            else
            {
                ProcessStatus = (int)AkeneoProductIndexerStatuses.ListingStarting;
                var Thread =  new Thread(() =>
                {
                    ListenerThread($"{Settings.BaseAkeneoUrl}{Settings.AkeneoProductCreateUrl}?limit=100");
                });
                Thread.Start();
            }
        }

        public void ListenerThread(string requestUrl)
        {
            ProcessStatus = (int) AkeneoProductIndexerStatuses.ListingInProgress;
            HttpManager = new BaseWebClientWriter();
            try
            {
                var response = HttpManager.GetData(requestUrl, new WebClientHeader("Authorization", $"Bearer {AuthToken}"));
                AkeneoIndexedProductDto dto = JsonConvert.DeserializeObject<AkeneoIndexedProductDto>(response);
                if (dto.LinksCollection.NextLink != null)
                {
                    var thread = new Thread(() =>
                    {
                        ListenerThread(dto.LinksCollection.NextLink.Href);
                    });
                    thread.Start();
                }

                if (dto.Embed.ItemsCollection != null && dto.Embed.ItemsCollection.Count > 0)
                {
                    var converter = new AkeneoBaseProductIndexerConverter();
                    foreach (var selected in dto.Embed.ItemsCollection)
                    {
                        try
                        {
                            var tempProductStorage = converter.ConvertToApplicationEntity(selected);
                            lock (RequestList)
                            {
                                RequestList.Add(tempProductStorage);
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.error(
                                $"(AkeneoBaseProductIndexerConverter exception): Cannot convert dto entity:  {e.Message}");
                        }
                    }
                }

                if (dto.LinksCollection.NextLink == null)
                {
                    ProcessStatus = (int)AkeneoProductIndexerStatuses.ListingFinished;
                    InvokeOnFinishedListing();
                }
            }
            catch (Exception e)
            {
                _logger.error(e.Message);
            }
        }

        private void InvokeOnFinishedListing()
        {
            OnFinishedListing?.Invoke(this, new PimProductsListenerCallbackResult(RequestList));
        }

        public event PimProductsListenerCallback OnFinishedListing;
    }
}