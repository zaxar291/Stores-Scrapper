using System;
using System.Collections.Generic;
using WebScrapper.Scrapper.Services.Shopify.Entities;

namespace WebScrapper.Scrapper.Services.Shopify.Delegates
{
    public delegate void ShopifyBaseWriterTask(object sender, ShopifyBaseWriterTaskResult eventArgs);
    public class ShopifyBaseWriterTaskResult
    {
        public ShopifyBaseWriterTaskResult(List<BaseShopifyProductEntity> list)
        {
            UrlsList = list;
        }
        List<BaseShopifyProductEntity> UrlsList { get; set; }
    }
}
