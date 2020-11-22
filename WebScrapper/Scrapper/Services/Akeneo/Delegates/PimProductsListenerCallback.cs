using System;
using System.Collections.Generic;
using WebApplication.Scrapper.Services.Akeneo.Entities;

namespace WebApplication.Scrapper.Services.Akeneo.Delegates
{

    public delegate void PimProductsListenerCallback(object sender, PimProductsListenerCallbackResult eventArgs);
    
    public class PimProductsListenerCallbackResult : EventArgs
    {
        public PimProductsListenerCallbackResult(List<AkeneoProduct> product)
        {
            indexedProductsList = product;
        }
        
        public List<AkeneoProduct> indexedProductsList { get; }
    }
}