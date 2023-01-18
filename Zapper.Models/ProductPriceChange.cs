namespace Zapper.Models
{
	public class ProductPriceChange
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public DateTime Changed { get; set; }
        public double? PreviousPrice { get; set; }
        public double? CurrentPrice { get; set; }

    }

}
