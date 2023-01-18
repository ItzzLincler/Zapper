using Newtonsoft.Json;

namespace Zapper.Api.Models
{
    [JsonObject(IsReference = true)]
    public class ProductPriceChange
    {
        public Guid Id { get; set; }
        public ScrapedProduct Product { get; set; }
        public DateTime Changed { get; set; }
        public double? PreviousPrice { get; set; }
        public double? CurrentPrice { get; set; }

        public override string ToString()
        {
            return $"At {Changed}: {PreviousPrice} -> {CurrentPrice}";
        }

    }

    public class ProductHistory
    {
        public List<ProductPriceChange> Changes { get; set; }
        public ScrapedProduct Product { get; set; }


    }

}
