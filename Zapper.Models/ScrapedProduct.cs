namespace Zapper.Models
{
    public class ScrapedProduct
    {
        public Guid Id { get; set; }
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
        public Uri ImageUri { get; set; }
        public string ImagePath { get; set; }
        public bool HasImage { get; set; } = false;
        public string? Manufacturer { get; set; }
        public string? ManufacturerSerial { get; set; }
    }

}
