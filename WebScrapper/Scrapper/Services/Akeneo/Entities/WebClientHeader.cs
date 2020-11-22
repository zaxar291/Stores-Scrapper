namespace WebApplication.Scrapper.Entities
{
    public class WebClientHeader
    {
        public string Name { get; set; }
        
        public string Value { get; set; }

        public WebClientHeader(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}