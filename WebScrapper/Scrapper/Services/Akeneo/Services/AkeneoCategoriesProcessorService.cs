using System;
using System.IO;
using System.Linq;

using Newtonsoft.Json;

using WebScrapper.Scrapper.Services.Akeneo.Abstraction;
using WebScrapper.Scrapper.Services.Akeneo.Entities;
using WebScrapper.Scrapper.Services.Akeneo.Enums;

using WebApplication.Scrapper.Services;
using WebApplication.Scrapper.Abstraction;

namespace WebScrapper.Scrapper.Services.Akeneo.Services
{
    public class AkeneoCategoriesProcessorService : IBaseAkeneoService
    {
        private string ServiceName { get; set; }
        private AbstractWriter _f { get; set; }
        private AkeneoCategoriesServiceSettings ServiceSettings { get; set; }
        private BaseLogger _l { get; set; }

        #region Rules
        private AkeneoCategoriesAssignationEntity[] CollectionsAssignations { get; set; }
        private AkeneoCategoriesExcludedCollectionEntity[] CollectionsExcludes { get; set; }
        private AkeneoCategoriesExcludedProductEntity[] ProductsExcludes { get; set; }
        #endregion

        public AkeneoCategoriesProcessorService(AkeneoCategoriesServiceSettings settings, BaseLogger logger)
        {
            ServiceSettings = settings;
            _f = new FilesWriter();            
            _l = logger;
            if (ReferenceEquals(ServiceSettings.BaseDir, null)
                || ReferenceEquals(ServiceSettings.BaseDir, String.Empty))
            {
                ServiceSettings.BaseDir = $@"{Directory.GetCurrentDirectory()}/Resources/Services/Akeneo/AkeneoCategoriesProcessorService/";
            }
        }

        public void Init() 
        {
            if (!ReferenceEquals(ServiceSettings.CollectionsAssignationsRules, null)
                && !ReferenceEquals(ServiceSettings.CollectionsAssignationsRules, String.Empty))
            {
                if (_f.IsFileExists($"{ServiceSettings.BaseDir}{ServiceSettings.CollectionsAssignationsRules}"))
                {
                    var temp = _f.Read($"{ServiceSettings.BaseDir}{ServiceSettings.CollectionsAssignationsRules}");
                    if (ReferenceEquals(temp, String.Empty))
                    {
                        CollectionsAssignations = new AkeneoCategoriesAssignationEntity[0];
                    }
                    else
                    {
                        try
                        {
                            CollectionsAssignations = JsonConvert.DeserializeObject<AkeneoCategoriesAssignationEntity[]>(temp);
                        }
                        catch (Exception e)
                        {
                            _l.warn($"AkeneoCategoriesProcessorService: cannot deserialize rules for collections assignations: {e.Message} -> {e.StackTrace}");
                            CollectionsAssignations = new AkeneoCategoriesAssignationEntity[0];
                        }
                    }
                }
            }
            if (!ReferenceEquals(ServiceSettings.CollectionsExcludedRules, null)
                && !ReferenceEquals(ServiceSettings.CollectionsExcludedRules, String.Empty))
            {
                if (_f.IsFileExists($"{ServiceSettings.BaseDir}{ServiceSettings.CollectionsExcludedRules}"))
                {
                    var temp = _f.Read($"{ServiceSettings.BaseDir}{ServiceSettings.CollectionsExcludedRules}");
                    if (ReferenceEquals(temp, String.Empty))
                    {
                        CollectionsExcludes = new AkeneoCategoriesExcludedCollectionEntity[0];
                    }
                    else
                    {
                        try
                        {
                            CollectionsExcludes = JsonConvert.DeserializeObject<AkeneoCategoriesExcludedCollectionEntity[]>(temp);
                        }
                        catch (Exception e)
                        {
                            _l.warn($"AkeneoCategoriesProcessorService: cannot deserialize rules for collections excludes: {e.Message} -> {e.StackTrace}");
                            CollectionsExcludes = new AkeneoCategoriesExcludedCollectionEntity[0];
                        }
                    }
                }
            }
            if (!ReferenceEquals(ServiceSettings.ProductsExcluded, null)
                && !ReferenceEquals(ServiceSettings.ProductsExcluded, String.Empty))
            {
                if (_f.IsFileExists($"{ServiceSettings.BaseDir}{ServiceSettings.ProductsExcluded}"))
                {
                    var temp = _f.Read($"{ServiceSettings.BaseDir}{ServiceSettings.ProductsExcluded}");
                    if (ReferenceEquals(temp, String.Empty))
                    {
                        ProductsExcludes = new AkeneoCategoriesExcludedProductEntity[0];
                    }
                    else
                    {
                        try
                        {
                            ProductsExcludes = JsonConvert.DeserializeObject<AkeneoCategoriesExcludedProductEntity[]>(temp);
                        }
                        catch (Exception e)
                        {
                            _l.warn($"AkeneoCategoriesProcessorService: cannot deserialize rules for product names excludes: {e.Message} -> {e.StackTrace}");
                            ProductsExcludes = new AkeneoCategoriesExcludedProductEntity[0];
                        }
                    }
                }
            }
        }

        public bool IsCollectionExcluded(string collectionName)
        {
            if (ReferenceEquals(CollectionsExcludes, null)
                || ReferenceEquals(CollectionsExcludes.Length, 0))
            {
                _l.warn("AkeneoCategoriesProcessorService: empty excludes rules for collections!");
                return false;
            }
            try
            {
                var selection = CollectionsExcludes.FirstOrDefault(c => c.Name.Trim().ToLower().Equals(collectionName.Trim().ToLower()));
                if (ReferenceEquals(selection, null))
                {
                    return false;
                }
                return true;
            }   
            catch (Exception e)
            {
                _l.error($"AkeneoCategoriesProcessorService: selection fatal error: {e.Message} -> {e.StackTrace}");
                return false;
            }
        }
        public string AssignCollection(string collectionName)
        {
            if (ReferenceEquals(CollectionsAssignations, null)
                || ReferenceEquals(CollectionsAssignations.Length, 0))
            {
                _l.warn("AkeneoCategoriesProcessorService: empty assignations rules for collections!");
                return String.Empty;
            }
            try
            {
                var selection = CollectionsAssignations.FirstOrDefault(c => c.CategoryToSearch.Trim().ToLower().Equals(collectionName.Trim().ToLower()));
                if (ReferenceEquals(selection, null))
                {
                    return String.Empty;
                }
                return selection.CategoryToAssign;
            }
            catch (Exception e)
            {
                _l.error($"AkeneoCategoriesProcessorService: selection fatal error: {e.Message} -> {e.StackTrace}");
                return String.Empty;
            }
        }
        public bool IsProductExcluded(string productName)
        {
            if (ReferenceEquals(ProductsExcludes, null)
                || ReferenceEquals(ProductsExcludes.Length, 0))
            {
                _l.warn("AkeneoCategoriesProcessorService: empty excludes rules for products!");
                return false;
            }
            try
            {
                var selection = ProductsExcludes.FirstOrDefault(p => productName.ToLower().Trim().Contains(p.Name.ToLower().Trim()));
                if (ReferenceEquals(selection, null))
                {
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                _l.error($"AkeneoCategoriesProcessorService: selection fatal error: {e.Message} -> {e.StackTrace}");
                return false;
            }
        }
        public string FindCollection(By name)
        {
            if (ReferenceEquals(CollectionsAssignations, null)
                || ReferenceEquals(CollectionsAssignations.Length, 0))
            {
                _l.warn("AkeneoCategoriesProcessorService: empty collections rules, nothing to search.");
                return String.Empty;
            }
            return String.Empty;
        }
        public string GetServiceName()
        {
            return ServiceName;
        }

        public void Dispose()
        {
            CollectionsAssignations = null;
            CollectionsExcludes = null;
            ProductsExcludes = null;
        }
    }
}