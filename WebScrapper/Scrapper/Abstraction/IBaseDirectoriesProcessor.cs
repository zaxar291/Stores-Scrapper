namespace WebScrapper.Scrapper.Abstraction
{
    public interface IBaseDirectoriesProcessor 
    {
        bool Create(string directory);
        bool Remove(string directory);
        bool Exists(string directory);

        string GetLastExcept();
    }
}