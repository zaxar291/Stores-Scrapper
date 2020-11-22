using System;
using System.Collections.Generic;

using WebScrapper.Scrapper.Delegates;
using WebScrapper.Scrapper.Entities;

using HtmlAgilityPack;

namespace WebApplication.Scrapper.Abstraction
{
    public abstract class AbstractLinksScrapper : IDisposable
    {
        protected abstract List<string> LinksPool { get; set; }
        protected abstract List<string> IndexedLinks { get; set; }
        protected abstract IBaseValidationService ValidationService { get; set;}
        protected abstract BaseLogger _l { get; set; }
        public abstract void LinksScrapperInstance();
        protected abstract void LinksScrapperThread(string l);
        protected abstract string PrepareLink(string link);
        protected abstract bool IsLinkExist(string l);
        protected abstract bool IsLinkIndexed(string l);
        protected abstract void ScrapProductFromUrl(string url, HtmlNode HtmlNode, WebScrapperBaseProxyEntity proxyInfo);
        public abstract event BaseScrapperChangeStatusCallback OnInstanceStatusUpdating;
        public abstract void Dispose();
    }
}