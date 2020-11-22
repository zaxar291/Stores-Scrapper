namespace WebApplication.Scrapper.Abstraction
{
    public interface IBaseValidationService {
        public bool Validate(string request);
        public string GetExceptMessage();
        public void SetBaseSiteUrl(string b);
    }
}