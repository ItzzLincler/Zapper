using Microsoft.EntityFrameworkCore;
using Zapper.Api.Models;

namespace Zapper.Api.Data
{
    public class ProductsContext : DbContext
    {
        public DbSet<ScrapedProduct> ScrapedProducts { get; set; }
        public DbSet<ProductPriceChange> ProductPriceChanges { get; set; }
        public ProductsContext(DbContextOptions<ProductsContext> options) : base(options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseNpgsql();

    }

    public class OldContext : DbContext
    {
        public DbSet<OriginalProduct> ScrapedProducts { get; set; }
        public DbSet<OriginalChange> ProductPriceChanges { get; set; }
        public OldContext(DbContextOptions<OldContext> options) : base(options)
        {

        }
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    modelBuilder.Entity<ProductPriceChange>().HasOne(p => )
        //}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseNpgsql();

    }

    public class OriginalChange
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public DateTime Changed { get; set; }
        public double? PreviousPrice { get; set; }
        public double? CurrentPrice { get; set; }

        public override string ToString()
        {
            return $"At {Changed}: {PreviousPrice} -> {CurrentPrice}";
        }

    }

    public class OriginalProduct
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
        public string? ImagePath { get; set; }
        public bool HasImage { get; set; } = false;
        public string? Manufacturer { get; set; }
        public string? ManufacturerSerial { get; set; }

    }
}
