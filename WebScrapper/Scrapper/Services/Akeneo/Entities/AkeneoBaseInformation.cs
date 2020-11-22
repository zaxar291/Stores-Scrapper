using System.Net.NetworkInformation;

namespace WebApplication.Scrapper.Services.Akeneo.Entities
{
    public class AkeneoBaseInformation
    {
        public readonly string AkeneoPasswordGrantType = "password";
        
        public readonly string AkeneoAuthUrl = "api/oauth/v1/token";
        public readonly string AkeneoProductCreateUrl = "api/rest/v1/products";
        public readonly string AkeneoCategoryListUrl = "api/rest/v1/categories";
        
        public string BaseAkeneoUrl { get; set; }
        
        public string BaseAkeneoClientId { get; set; }
        
        public string BaseAkeneoSecretKey { get; set; }
        
        public string BaseAkeneoUserName { get; set; }
        
        public string BaseAkeneoPassword { get; set; }
    }
}