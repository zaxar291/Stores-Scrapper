using System;

namespace WebScrapper.Scrapper.Services.Akeneo.Abstraction
{
    public interface IBaseAkeneoService : IDisposable
    {
        string GetServiceName();
        void Init();
    }
}