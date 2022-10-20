using Microsoft.EntityFrameworkCore;
using Zapper.Models;

namespace Zapper.Data
{
    public class ScrapedProductsContext : DbContext
    {
        public DbSet<ScrapedProduct> ScrapedProducts { get; set; }
        public DbSet<ProductPriceChange> ProductPriceChanges { get; set; }
        public ScrapedProductsContext(DbContextOptions<ScrapedProductsContext> options) : base(options)
        {

        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseNpgsql();
      

    }
}
