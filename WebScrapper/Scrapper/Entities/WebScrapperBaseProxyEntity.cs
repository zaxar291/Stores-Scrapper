namespace WebScrapper.Scrapper.Entities
{
    public class WebScrapperBaseProxyEntity
    {
        public bool IsProxyAvailable { get; set; }
        public string ProxyUrl { get; set; }
        public int ProxyPort { get; set; }
        public bool UseAuthorization { get; set; }
        public string AuthLogin { get; set; }
        public string AuthPassword { get; set; }
    }
}