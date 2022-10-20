
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Zapper.Api.Models
{
    public class ScrapedProduct
    {
        public Guid Id { get; set; }
        //public string SellerID { get; set; }
        public string Name { get; set; }
        public string Cat { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastChecked { get; set; }
        public DateTime LastChanged { get; set; }
        public double? CurrentPrice { get; set; }
        public double? LowestPrice { get; set; }
        public double? HighestPrice { get; set; }
        public string ProductLink { get; set; }
        public ScrapedProductSource ProductSource { get; set; }
        public string ProductType { get; set; }
        public string Manufacturer { get; set; }
        public Uri ImageUri { get; set; }
        public bool HasImage { get;set; } = false;
    }

    public class ProductPriceChange
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public DateTime Changed { get; set; }
        public double? PreviousPrice { get; set; }
        public double? CurrentPrice { get; set; }

    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ScrapedProductSource
    {
        TMS,
        KSP,
        Ivory,
        Bug,
        PC_Center,
        SHIRION,
        AA_Computers,
        Images
    }

}
