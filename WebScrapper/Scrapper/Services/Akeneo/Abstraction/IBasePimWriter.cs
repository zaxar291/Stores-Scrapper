using WebScrapper.Scrapper.Abstraction;
using WebApplication.Scrapper.Services.Akeneo.Enums;
using WebApplication.Scrapper.Services.Akeneo.Delegates;

using WebScrapper.Scrapper.Delegates;

namespace WebApplication.Scrapper.Services.Akeneo.Abstraction
{
    public interface IBasePimWriter <TEntity> : IBaseService 
    {
        bool BasePost(TEntity product, string requestUrl);
        bool BasePatch(TEntity product, string requestUrl);
        bool BaseSend(TEntity product, string requestUrl, string protocol);
        bool BaseGet(TEntity product, string requestUrl);
        void LaunchInstance();
        bool ProcessProduct(TEntity product);
        bool IsProductExists(string product);
        TEntity Preprocess(TEntity product);
        bool CreateNewProduct(TEntity product);
        bool UpdateExistsProduct(TEntity product);
        
        event BaseServiceCallBack OnProductListeningFinished;
    }
    
}