namespace WebApplication.Scrapper.Abstraction
{
    interface IScrapperBaseValidator
    {
        public bool ValidateWebSelector(string selector);
    }
}