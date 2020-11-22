using System;
using System.Collections.Generic;

namespace WebApplication.Scrapper.Services.Akeneo.Entities.Dto
{
    public class AkeneoProductDto
    {
        public string identifier { get; set; }
        
        public bool enabled { get; set; }
        
        public string family { get; set; }

        public List<string> categories { get; set; }
        
        public List<string> groups { get; set; }
        
        public string parent { get; set; }
        
        public object values { get; set; }
    }

    public class AkeneoProductDtoValueTextEntity
    {
        public string data { get; set; }
        public string locale { get; set; }
        public string scope { get; set; }
    }

    public class AkeneoProductDtoValuePriceDescriptionEntity
    {
        public string locale { get; set; }
        public string scope { get; set; }
        public List<AkeneoProductDtoValuePriceValuesEntity> data;
    }

    public class AkeneoProductDtoValuePriceValuesEntity
    {
        public string amount { get; set; }
        public string currency { get; set; }
    }
}