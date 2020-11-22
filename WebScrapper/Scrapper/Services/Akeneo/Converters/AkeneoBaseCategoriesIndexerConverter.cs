using System;
using System.Linq;
using System.Collections.Generic;

using WebApplication.Scrapper.Services.Akeneo.Abstraction;
using WebScrapper.Scrapper.Services.Akeneo.Entities;
using WebApplication.Scrapper.Services.Akeneo.Entities.Dto.IndexedCategoriesDto;

namespace WebApplication.Scrapper.Services.Akeneo.Converters
{
    internal sealed class AkeneoBaseCategoriesIndexerConverter : BaseConverter<AkeneoIndexedCategoriesDtoEmbedCollectionItem, AkeneoCategory>
    {
        public override AkeneoCategory ConvertToApplicationEntity(AkeneoIndexedCategoriesDtoEmbedCollectionItem dto)
        {
            var AkeneoCategory = new AkeneoCategory();
            AkeneoCategory.CategoryId = dto.CategoryCode;
            
            var categoryName = dto.CategoriesLocales.FirstOrDefault(c => c.Key.Equals("en_US"));
            if (!ReferenceEquals(categoryName, null) && !ReferenceEquals(categoryName.Key, null))
            {
                AkeneoCategory.CategoryName = categoryName.Value;
            }

            return AkeneoCategory;
        }

        public override AkeneoIndexedCategoriesDtoEmbedCollectionItem ConvertToDtoEntity(AkeneoCategory data)
        {
            var dto = new AkeneoIndexedCategoriesDtoEmbedCollectionItem();
            dto.CategoryParent = null;
            dto.CategoryCode = data.CategoryId;
            dto.CategoriesLocales = new Dictionary<string, string>();
            dto.CategoriesLocales.Add("en_US", data.CategoryName);
            return dto;
        }
    }
}