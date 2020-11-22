namespace WebScrapper.Scrapper.Abstraction.Classes
{
    interface IBaseProcessingSerivce<T> : IBaseService
    {
        string ProcessRule(string input, T rule);
    }
}
