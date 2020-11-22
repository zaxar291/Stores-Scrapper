
using Newtonsoft.Json;

namespace WebScrapper.Scrapper.Services.Shopify.Entities.Dto
{
    class ShopifyApiResponseDtoBaseProductVariant
    {
        [JsonProperty("id")]
        public string VariantIdentifier { get; set; }

        [JsonProperty("product_id")]
        public string VariantProductIdentifier { get; set; }

        [JsonProperty("price")]
        public string VariantPrice { get; set; }

        [JsonProperty("sku")]
        public string VariantSku { get; set; }

        [JsonProperty("position")]
        public string VariantPosition { get; set; }

        [JsonProperty("inventory_policy")]
        public string VariantInventoryPolicy { get; set; }

        [JsonProperty("compare_at_price")]
        public string VariantCompareatPrice { get; set; }

        [JsonProperty("fulfillment_service")]
        public string VariantFulfillmentService{ get; set; }

        [JsonProperty("inventory_management")]
        public string VariantInventoryManagment{ get; set; }

        [JsonProperty("option1")]
        public string VariantOptionOne{ get; set; }

        [JsonProperty("option2")]
        public string VariantOptionTwo { get; set; }

        [JsonProperty("option3")]
        public string VariantOptionThree { get; set; }

        [JsonProperty("created_at")]
        public string VariantCreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public string VariantUpdatedAt{ get; set; }

        [JsonProperty("taxable")]
        public bool VariantTaxable { get; set; }

        [JsonProperty("barcode")]
        public string VariantBarCode { get; set; }

        [JsonProperty("grams")]
        public int VariantGrams{ get; set; }

        [JsonProperty("image_id")]
        public string VariantImageId { get; set; }

        [JsonProperty("weight")]
        public string VariantWeight { get; set; }

        [JsonProperty("weight_unit")]
        public string VariantWeightUnit { get; set; }

        [JsonProperty("inventory_item_id")]
        public string VariantInventoryItemId { get; set; }

        [JsonProperty("inventory_quantity")]
        public string VariantInventoryQuantity{ get; set; }

        [JsonProperty("old_inventory_quantity")]
        public string VariantOldInventoryQuantity{ get; set; }

        [JsonProperty("requires_shipping")]
        public bool VariantRequiresShipping{ get; set; }

        [JsonProperty("admin_graphql_api_id")]
        public string VariantAdminGraphqlApiId { get; set; }
    }
}
