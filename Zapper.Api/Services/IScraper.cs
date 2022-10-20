using Zapper.Api.Models;

namespace Zapper.Api.Services
{
    public interface IScraper
    {
        public Task<List<ScrapedProduct>> ScrapeProducts(CancellationToken stoppingToken);

        public ScrapedProductSource GetSource();

        public Task ResolveMissingImagesAsync(IEnumerable<ScrapedProduct> scrapedProducts);

    }
}
