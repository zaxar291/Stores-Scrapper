using Newtonsoft.Json;

namespace WebApplication.Scrapper.Entities.BaseScrapperEntities {

    public class BaseScrapperSchemaEntity {

        [JsonProperty("@context")]
        public string SchemaContext { get; set; }

        [JsonProperty("@type")]
        public string SchemaType { get; set; }

        [JsonProperty("name")]
        public string SchemaName { get; set; }

        [JsonProperty("image")]
        public string SchemaImage { get; set; }

        [JsonProperty("sku")]
        public string SchemaSku { get; set; }

        [JsonProperty("description")]
        public string SchemaDescription { get; set; }

        [JsonProperty("brand")]
        public BaseScrapperSchemaEntityBrand SchemaBrand { get; set; }
        public string ProductBrand { get; set; }
        [JsonProperty("offers")]
        public BaseScrapperSchemaEntityOffer Offer { get; set; }
        public string Price { get; set; }
    }

    public class BaseScrapperSchemaEntityBrand {
        [JsonProperty("@type")]
        public string FieldType { get; set; }
        [JsonProperty("name")]
        public string FieldValue { get; set; }
    }

    public class BaseScrapperSchemaEntityOffer {
        [JsonProperty("price")]
        public string Price { get; set; }
    }
}