using Newtonsoft.Json;

namespace Zapper.Api.Data.Scrapers.Ksp
{
    public partial class BmsResponse
    {
        [JsonProperty("result")]
        public Dictionary<string, ProductBms> Result { get; set; }
    }

    public partial class ProductBms
    {
        [JsonProperty("uin")]
        public long Uin { get; set; }

        [JsonProperty("price")]
        public long Price { get; set; }

        [JsonProperty("icons")]
        public Uri[] Icons { get; set; }

        [JsonProperty("triggered")]
        public Triggered[] Triggered { get; set; }

        [JsonProperty("discount")]
        public Discount Discount { get; set; }

        [JsonProperty("eilat_price")]
        public long EilatPrice { get; set; }

        [JsonProperty("cheaperPriceViaPhone")]
        public long CheaperPriceViaPhone { get; set; }
    }

    public partial class Discount
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("start")]
        public string Start { get; set; }

        [JsonProperty("end")]
        public string End { get; set; }

        [JsonProperty("store_discount")]
        public bool StoreDiscount { get; set; }

        [JsonProperty("cart_discount")]
        public bool CartDiscount { get; set; }

        [JsonProperty("value")]
        public long Value { get; set; }
    }

    public partial class Triggered
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("campaignId")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long CampaignId { get; set; }

        [JsonProperty("banner")]
        public Uri Banner { get; set; }

        [JsonProperty("href")]
        public Uri Href { get; set; }
    }
}
