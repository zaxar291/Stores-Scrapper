using System.Collections.Generic;

namespace WebApplication.Scrapper.Services.Akeneo.Entities
{
    public class AkeneoProduct
    {
        public string productUrl { get; set; } // auto
        public string productName { get; set; }
        public string imageUrl { get; set; }
        public string productVendor { get; set; }
        public string productCategory { get; set; }
        public string productDescription { get; set; }
        public string productId { get; set; } //auto
        public string productCode { get; set; }
        public string productPrice { get; set; }
        public string productSalePrice { get; set; }
        public bool isProductInStock { get; set; }
        public bool isProcessed { get; set; }
        public string productFamily { get; set; } // Auto
        public string productTags { get; set; }
        public List<string> imagesList { get; set; } // ToDo
    }
}