using System.Collections.Generic;
using Newtonsoft.Json;

namespace WebApplication.Scrapper.Services.Akeneo.Entities.Dto.IndexersDto
{
    public class AkeneoIndexedProductDtoEmbedCollectionItemValues
    {
        [JsonProperty("price")]
        public List<AkeneoIndexedProductDtoEmbedCollectionItemValuesPriceEntity> ProductPrice { get; set; }
        [JsonProperty("saleprice")]
        public List<AkeneoIndexedProductDtoEmbedCollectionItemValuesPriceEntity> ProductPriceSale { get; set; }
        [JsonProperty("name")]
        public List<AkeneoIndexedProductDtoEmbedCollectionItemValuesTextEntity>ProductName { get; set; }
        [JsonProperty("brand")]
        public List<AkeneoIndexedProductDtoEmbedCollectionItemValuesTextEntity> ProductBrand { get; set; }
        [JsonProperty("imageurl")]
        public List<AkeneoIndexedProductDtoEmbedCollectionItemValuesTextEntity> ProductImageUrl { get; set; }
        [JsonProperty("producturl")]
        public List<AkeneoIndexedProductDtoEmbedCollectionItemValuesTextEntity> ProductUrl { get; set; }
        [JsonProperty("product_code")]
        public List<AkeneoIndexedProductDtoEmbedCollectionItemValuesTextEntity> ProductCode { get; set; }
        [JsonProperty("product_tags")]
        public List<AkeneoIndexedProductDtoEmbedCollectionItemValuesTextEntity> ProductTags { get; set; }
        [JsonProperty("product_category")]
        public List<AkeneoIndexedProductDtoEmbedCollectionItemValuesTextEntity> ProductCategory { get; set; }
        [JsonProperty("Description")]
        public List<AkeneoIndexedProductDtoEmbedCollectionItemValuesTextEntity> ProductDescription { get; set; }
        [JsonProperty("imageslinks")]
        public List<AkeneoIndexedProductDtoEmbedCollectionItemValuesTextEntity>ProductImagesLinks { get; set; }
    }
}