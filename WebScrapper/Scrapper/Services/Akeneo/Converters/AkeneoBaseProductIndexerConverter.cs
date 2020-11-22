using System;
using System.Collections.Generic;
using System.Linq;

using WebApplication.Scrapper.Services.Akeneo.Abstraction;
using WebApplication.Scrapper.Services.Akeneo.Entities;
using WebApplication.Scrapper.Services.Akeneo.Entities.Dto.IndexersDto;

namespace WebApplication.Scrapper.Services.Akeneo.Converters
{
    public class AkeneoBaseProductIndexerConverter : BaseConverter<AkeneoIndexedProductDtoEmbedCollectionItem, AkeneoProduct>
    {
        public override AkeneoProduct ConvertToApplicationEntity(AkeneoIndexedProductDtoEmbedCollectionItem dto)
        {
            AkeneoProduct convertedProduct = new AkeneoProduct();
            convertedProduct.isProcessed = true;
            try
            {
                convertedProduct.productUrl = dto.ProductValues.ProductUrl.FirstOrDefault().EntityData;
                try
                {
                    convertedProduct.productName = dto.ProductValues.ProductName.FirstOrDefault().EntityData;
                }
                catch (Exception)
                {
                    convertedProduct.productName = String.Empty;
                }
                try
                {
                    convertedProduct.imageUrl = dto.ProductValues.ProductImageUrl.FirstOrDefault().EntityData;
                }
                catch (Exception)
                {
                    convertedProduct.imageUrl  = String.Empty;
                }
                
                try
                {
                    convertedProduct.productVendor = dto.ProductValues.ProductBrand.FirstOrDefault().EntityData;
                }
                catch (Exception)
                {
                    convertedProduct.productVendor = String.Empty;
                }
               
                try
                {
                    convertedProduct.productCategory = dto.ProductValues.ProductCategory.FirstOrDefault().EntityData;
                }
                catch (Exception)
                {
                    convertedProduct.productCategory = String.Empty;
                }

                try
                {
                    convertedProduct.productDescription =
                        dto.ProductValues.ProductDescription.FirstOrDefault().EntityData;
                }
                catch (Exception)
                {
                    convertedProduct.productDescription = String.Empty;
                }
                convertedProduct.productId = dto.Identifier;
                try
                {
                    convertedProduct.productCode = dto.ProductValues.ProductCode.FirstOrDefault().EntityData;
                }
                catch (Exception)
                {
                    convertedProduct.productCode = String.Empty;
                }
                
                try
                {
                    convertedProduct.productPrice = dto.ProductValues.ProductPrice.FirstOrDefault().EntityData
                        .FirstOrDefault().EntityAmount.ToString();
                }
                catch (Exception)
                {
                    convertedProduct.productPrice = "0";
                }

                try
                {
                    convertedProduct.productSalePrice = dto.ProductValues.ProductPriceSale.FirstOrDefault().EntityData
                        .FirstOrDefault().EntityAmount.ToString();
                }
                catch (Exception)
                {
                    convertedProduct.productSalePrice = "0";
                }
                convertedProduct.isProductInStock = dto.Enabled;
                convertedProduct.isProcessed = true;
                convertedProduct.productFamily = dto.ProductFamily;
                try
                {
                    convertedProduct.productTags = dto.ProductValues.ProductTags.FirstOrDefault().EntityData;
                }
                catch (Exception)
                {
                    convertedProduct.productTags = String.Empty;
                }
                
                if (dto.ProductValues.ProductImagesLinks != null)
                {
                    convertedProduct.imagesList = new List<string>();
                    var images = dto.ProductValues.ProductImagesLinks.FirstOrDefault().EntityData.Split(",");
                    if (images.Length > 0)
                    {
                        foreach (var image in images)
                        {
                            convertedProduct.imagesList.Add(image);
                        }
                    }
                    else if (dto.ProductValues.ProductImagesLinks.FirstOrDefault().EntityData.Trim() != String.Empty)
                    {
                        convertedProduct.imagesList.Add(
                            dto.ProductValues.ProductImagesLinks.FirstOrDefault().EntityData);
                    }
                }
                else
                {
                    convertedProduct.imagesList = new List<string>();
                }
            }
            catch (NullReferenceException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }
            return convertedProduct;
        }
    }
}