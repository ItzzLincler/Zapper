using Zapper.Api.Models;
using Zapper.Api.Services.Scrapers;

namespace Zapper.Api.Services
{
    public interface IScraper
    {
        public Task<List<ScrapedProduct>> ScrapeProductsAsync(CancellationToken stoppingToken);

        public Task ResolveMissingImagesAsync(IEnumerable<ScrapedProduct> scrapedProducts);


    }

    public abstract class ScraperBase : IScraper
    {
        public bool IsRunning { get; protected set; }
        public ScrapedProductSource Source { get; protected init; }

        protected IEnumerable<ScrapeableLink> targetLinks { get; init; }
        protected ILogger<ScraperBase> logger { get; init; }
        public async Task<List<ScrapedProduct>> ScrapeProductsAsync(CancellationToken token)
        {
            IsRunning = true;
            List<ScrapedProduct> result = new();
            try
            {
                logger.LogInformation($"Scraping products for {Source}");
                foreach (var link in targetLinks)
                {
                    var partialProducts = await ScrapeProductAsync(link, token);
                    result.AddRange(partialProducts);
                    logger.LogInformation($"Scraped {partialProducts.Count} items of prodcut type: {link.ProductType} from {Source}");
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to scrape from: {Source}", ex);
            }
            finally
            {
                IsRunning = false;
            }
            return result;
        }

        protected abstract Task<List<ScrapedProduct>> ScrapeProductAsync(ScrapeableLink link, CancellationToken token);

        public Task ResolveMissingImagesAsync(IEnumerable<ScrapedProduct> scrapedProducts)
        {
            throw new NotImplementedException();
        }
    }
}
