using System;
using System.Collections.Generic;
using WebApplication.Scrapper.Services.Akeneo.Delegates;

namespace WebApplication.Scrapper.Services.Akeneo.Abstraction
{
    public interface IBaseListener <T, C>
    {
        C Settings { get; }
        List<T> RequestList { get; set; }
        bool IsIndexationFinished();
        void List();
        event PimProductsListenerCallback OnFinishedListing;
    }
}