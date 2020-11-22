using System;
using System.Collections.Generic;
using System.Net;

using WebApplication.Scrapper.Services.Akeneo.Abstraction;
using WebApplication.Scrapper.Services.Akeneo.Entities;
using WebApplication.Scrapper.Services.Akeneo.Entities.Dto;

namespace WebApplication.Scrapper.Services.Akeneo.Converters
{
    public class AkeneoBaseProductEntityConverter : BaseConverter<AkeneoProductDto, AkeneoProduct>
    { 
        public override AkeneoProductDto ConvertToDtoEntity(AkeneoProduct data)
        {
            AkeneoProductDto productDto = new AkeneoProductDto();
            productDto.identifier = data.productId;
            productDto.enabled = data.isProductInStock;
            productDto.family = data.productFamily;
            productDto.categories = new List<string>();
            productDto.groups = new List<string>();
            productDto.parent = null;

            productDto.categories.Add(data.productCategory);
            
            AkeneoProductDtoValueTextEntity name = new AkeneoProductDtoValueTextEntity();
            name.data = data.productName;
            name.locale = null;
            name.scope = null;
            
            AkeneoProductDtoValueTextEntity product_tags = new AkeneoProductDtoValueTextEntity();
            product_tags.data = data.productTags;
            product_tags.locale = null;
            product_tags.scope = null;
            
            AkeneoProductDtoValueTextEntity product_code = new AkeneoProductDtoValueTextEntity();
            product_code.data = data.productCode;
            product_code.locale = null;
            product_code.scope = null;

            AkeneoProductDtoValueTextEntity imageurl = new AkeneoProductDtoValueTextEntity();
            imageurl.data = $"{data.imageUrl}";
            imageurl.locale = null;
            imageurl.scope = null;
            
            AkeneoProductDtoValueTextEntity producturl = new AkeneoProductDtoValueTextEntity();
            producturl.data = data.productUrl;
            producturl.locale = null;
            producturl.scope = null;
            
            AkeneoProductDtoValueTextEntity brand = new AkeneoProductDtoValueTextEntity();
            brand.data = data.productVendor;
            brand.locale = null;
            brand.scope = null;
            
            // AkeneoProductDtoValueTextEntity product_category = new AkeneoProductDtoValueTextEntity();
            // product_category.data = data.productCategory;
            // product_category.locale = null;
            // product_category.scope = null;
            
            AkeneoProductDtoValueTextEntity imageslinks = new AkeneoProductDtoValueTextEntity();
            imageslinks.locale = null;
            imageslinks.scope = null;

            if (data.imagesList != null && data.imagesList.Count > 0)
            {
                imageslinks.data = string.Join(",", data.imagesList);
            }
            else
            {
                imageslinks.data = String.Empty;
            }
            
            AkeneoProductDtoValueTextEntity Description = new AkeneoProductDtoValueTextEntity();
            Description.data = data.productDescription;
            Description.locale = null;
            Description.scope = null;
            
            AkeneoProductDtoValuePriceDescriptionEntity price = new AkeneoProductDtoValuePriceDescriptionEntity();
            price.locale = null;
            price.scope = null;
            price.data = new List<AkeneoProductDtoValuePriceValuesEntity>();
            
            AkeneoProductDtoValuePriceValuesEntity priceValues = new AkeneoProductDtoValuePriceValuesEntity();
            if (!ReferenceEquals(data.productPrice, null)) 
            {
                data.productPrice = WebUtility.HtmlDecode(data.productPrice);
                priceValues.amount = data.productPrice.Replace("$", "").Replace("USD", "").Trim();
            }
            else 
            {
                priceValues.amount = "0";
            }
            
            priceValues.currency = "USD";
            
            price.data.Add(priceValues);
            
            AkeneoProductDtoValuePriceDescriptionEntity priceOld = new AkeneoProductDtoValuePriceDescriptionEntity();
            priceOld.locale = null;
            priceOld.scope = null;
            priceOld.data = new List<AkeneoProductDtoValuePriceValuesEntity>();
            
            AkeneoProductDtoValuePriceValuesEntity priceOldValues = new AkeneoProductDtoValuePriceValuesEntity();
            if (ReferenceEquals(data.productSalePrice, null)) {
                priceOldValues.amount = "0";
            } else {
                priceOldValues.amount = data.productSalePrice.Replace("$", "").Replace("USD", "").Trim();
            }
            priceOldValues.currency = "USD";
            
            priceOld.data.Add(priceOldValues);
            
            productDto.values = new 
            {
                name = new object[] {name},
                product_tags = new object[] {product_tags},
                product_code = new object[] {product_code},
                imageslinks = new object[] {imageslinks},
                imageurl = new object[] {imageurl},
                producturl = new object[] {producturl},
                brand = new object[] {brand},
                Description = new object[] {Description},
                price = new object[] {price},
                saleprice = new object[] {priceOld}
            };
            return productDto;
        }
    }
}