using System;

namespace WebApplication.Scrapper.Services.Akeneo.Delegates
{
    public delegate void PimProductReadyStatusCallback(object sender, PimProductReadyStatusCallbackResult eventArgs);
    public class PimProductReadyStatusCallbackResult : EventArgs
    {

        public PimProductReadyStatusCallbackResult(bool result)
        {
            IsAkeneoProductsListeningFinished = result;
        }
        
        public bool IsAkeneoProductsListeningFinished { get; set; }
    }
}