using HtmlAgilityPack;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Zapper.Data;
using Zapper.Models;

namespace Zapper.Services
{
    public class ScraperService
    {

        private readonly ILogger<ScraperService> _logger;

        public ScraperService(ILogger<ScraperService> logger)
        {
            _logger = logger;
            logger.LogInformation("Ctor called");
        }

        public async Task ScrapeAsync()
        {
            await Task.Delay(200);
            _logger.LogInformation("Data scraped");
        }


    }
    record PeriodicHostedServiceState(bool IsEnabled);
    public class PeriodicScrapeService : BackgroundService
    {
        private readonly ILogger<PeriodicScrapeService> _logger;
        private readonly IServiceScopeFactory _factory;
        private readonly TimeSpan _period = TimeSpan.FromHours(8);
        private int _executionCount = 0;
        public bool IsEnabled { get; set; }
        public PeriodicScrapeService(ILogger<PeriodicScrapeService> logger, IServiceScopeFactory factory)
        {
            _logger = logger;
            _factory = factory;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            using PeriodicTimer timer = new PeriodicTimer(_period);
            await using AsyncServiceScope asyncScope = _factory.CreateAsyncScope();
            //ScraperService scraper = asyncScope.ServiceProvider.GetRequiredService<ScraperService>();
            var scrapedProductsContext = asyncScope.ServiceProvider.GetRequiredService<ScrapedProductsContext>();
            var scrapers = asyncScope.ServiceProvider.GetServices<IScraper>();
            do
            {
                try
                {
                    var scrapedProducts = await ScrapeAllAsync(scrapers, cancellationToken);
                    await UpdateData(scrapedProducts, scrapedProductsContext, cancellationToken);

                    _executionCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to execute {nameof(PeriodicScrapeService)} with message: {ex.Message} ", ex);
                }
            } while (!cancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync(cancellationToken));
        }

        private async Task<List<ScrapedProduct>> ScrapeAllAsync(IEnumerable<IScraper> scrapers, CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting scraping round:");
            List<Task<List<ScrapedProduct>>> scrapingTasks = new List<Task<List<ScrapedProduct>>>();
            foreach (var scraper in scrapers)
                scrapingTasks.Add(scraper.ScrapeProducts(stoppingToken));
            await Task.WhenAll(scrapingTasks);
            var result = new List<ScrapedProduct>();
            scrapingTasks.ForEach(t => result.AddRange(t.Result));
            _logger.LogInformation($"Finished scraping successfuly - Count: {_executionCount}");
            return result;
        }
        private async Task UpdateData(List<ScrapedProduct> scrapedProducts, ScrapedProductsContext scrapedProductsContext, CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var currentProducts = await scrapedProductsContext.ScrapedProducts.ToListAsync();
            var newProducts = new List<ScrapedProduct>();
            var newChanges = new List<ProductPriceChange>();
            await Task.Run(() =>
            {
                foreach (var scrapedProduct in scrapedProducts)
                {
                    var current = currentProducts.SingleOrDefault(p => p.ProductSource == scrapedProduct.ProductSource && p.Cat == scrapedProduct.Cat);
                    if (current == null)
                        newProducts.Add(scrapedProduct);
                    else
                    {
                        if (current.CurrentPrice != scrapedProduct.CurrentPrice)
                            newChanges.Add(GetChange(current, scrapedProduct));
                        current.LastChecked = scrapedProduct.LastChecked;
                    }
                }
            });
            await scrapedProductsContext.ScrapedProducts.AddRangeAsync(newProducts);
            await scrapedProductsContext.ProductPriceChanges.AddRangeAsync(newChanges);
            await scrapedProductsContext.SaveChangesAsync();
            stopwatch.Stop();
            _logger.LogInformation($"Updated scraped data in {stopwatch.Elapsed}");
            return;
        }

        private ProductPriceChange GetChange(ScrapedProduct current, ScrapedProduct scrapedProduct)
        {
            var result = new ProductPriceChange
            {
                Changed = scrapedProduct.LastChanged,
                CurrentPrice = scrapedProduct.CurrentPrice,
                PreviousPrice = current.CurrentPrice,
                Id = new Guid(),
                ProductId = current.Id
            };
            current.LastChanged = result.Changed;
            current.CurrentPrice = result.CurrentPrice;
            current.LowestPrice = MinPrice(current.LowestPrice, result.CurrentPrice);
            current.HighestPrice = MaxPrice(current.HighestPrice, result.CurrentPrice);
            return result;

        }

        private double? MaxPrice(double? oldPrice, double? newPrice)
        {
            if (oldPrice == null && newPrice == null) return null; //safety not supposed to happen.
            if (oldPrice == null) return newPrice;
            if (newPrice == null) return oldPrice;
            return Math.Max(oldPrice.Value, newPrice.Value);
        }

        private double? MinPrice(double? oldPrice, double? newPrice)
        {
            if (oldPrice == null && newPrice == null) return null;
            if (oldPrice == null) return newPrice;
            if (newPrice == null) return oldPrice;
            return Math.Min(oldPrice.Value, newPrice.Value);
        }
    }

    public interface IScraper
    {
        public Task<List<ScrapedProduct>> ScrapeProducts(CancellationToken stoppingToken);
    }

    public class TmsScraper : IScraper
    {
        private readonly Dictionary<string, string> targetProducts = new Dictionary<string, string> {
         {"Graphics Cards","https://tms.co.il/computer-hardware-components/video-cards?limit=100"},
         {"Processors","https://tms.co.il/computer-hardware-components/prossesor?limit=100"},
         {"Random Access Memory","https://tms.co.il/computer-hardware-components/memory?limit=100"},
         {"HDD","https://tms.co.il/computer-hardware-components/hard-drives?limit=100"},
         {"SSD","https://tms.co.il/computer-hardware-components/ssd-drives?limit=100" }
        };
        private readonly string PaginationXpath = "/html/body/div[1]/div/div/div[2]/div[6]/div[1]/ul";
        private readonly string ProducstsXpath = "/html/body/div[1]/div/div/div[2]/div[5]";
        private readonly ILogger<TmsScraper> _logger;
        private readonly ScrapedProductsContext _productsContext;

        public TmsScraper(ScrapedProductsContext productsContext, ILogger<TmsScraper> logger)
        {
            _productsContext = productsContext;
            _logger = logger;
        }

        public async Task<List<ScrapedProduct>> ScrapeProducts(CancellationToken stoppingToken)
        {
            List<Task<List<ScrapedProduct>>> scrapingTasks = new List<Task<List<ScrapedProduct>>>();
            List<ScrapedProduct> scrapedProducts = new List<ScrapedProduct>();
            foreach (var product in targetProducts)
                scrapingTasks.Add(ScrapeProdcutType(product));
            await Task.WhenAll(scrapingTasks);
            foreach (var t in scrapingTasks)
                scrapedProducts.AddRange(t.Result);
            _logger.LogInformation($"Total products scraped from TMS: {scrapedProducts.Count}#");
            return scrapedProducts;
        }

        private async Task<List<ScrapedProduct>> ScrapeProdcutType(KeyValuePair<string, string> product)
        {
            HtmlWeb web = new HtmlWeb();
            StampedHtmlDocument stampedDocument = null;
            List<ScrapedProduct> scrapedProducts = new List<ScrapedProduct>();
            List<List<ScrapedProduct>> scrapedResults = new List<List<ScrapedProduct>>();
            try
            {
                await Task.Run(() => stampedDocument = new(web.Load(product.Value)));
                var result = await ScrapePage(stampedDocument, product);
                scrapedResults.Add(result);
                scrapedProducts.AddRange(result);
                int? pageCount = TryGetPageCount(stampedDocument);
                if (pageCount is null)
                    return result;
                for (int page = 2; page <= pageCount; page++)
                {
                    result = await ScrapePage(page, product);
                    scrapedResults.Add(result);
                    scrapedProducts.AddRange(result);
                }
                _logger.LogInformation($"Scraped {scrapedProducts.Count} items of prodcut type: {product.Key} from TMS");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return scrapedProducts;
        }

        private async Task<List<ScrapedProduct>> ScrapePage(StampedHtmlDocument document, KeyValuePair<string, string> product)
        {
            var productsNode = document.Document.DocumentNode.SelectSingleNode(ProducstsXpath);
            List<ScrapedProduct> scrapedProducts = new List<ScrapedProduct>();
            List<Task> scrapingTasks = new List<Task>();
            var productsNodes = productsNode.ChildNodes.Where(n => n.HasClass("product-card")).ToList();
            _logger.LogInformation($"Scraping products of type {product.Key}");
            await Task.Run(() =>
            {
                foreach (var productNode in productsNodes)
                {
                    var result = TryGetProductFromNode(productNode, product.Key);
                    if (result != null)
                    {
                        result.LastChanged = document.Stamp;
                        result.LastChecked = document.Stamp;
                        result.CreationDate = document.Stamp;
                        scrapedProducts.Add(result);
                    }
                    else
                        _logger.LogInformation("Failed to scrape product");
                }
            });
            await Task.WhenAll(scrapingTasks);
            return scrapedProducts;
        }

        private async Task<List<ScrapedProduct>> ScrapePage(int page, KeyValuePair<string, string> product)
        {
            HtmlWeb web = new HtmlWeb();
            StampedHtmlDocument stampedDocument = null;
            string pageUrl = product.Value + $"&page={page}";
            await Task.Run(() => stampedDocument = new(web.Load(pageUrl)));
            return await ScrapePage(stampedDocument, product);
        }

        private ScrapedProduct TryGetProductFromNode(HtmlNode node, string productType)
        {
            try
            {
                var children = node.ChildNodes.Where(n => n.Name == "div").ToArray();
                var header = children[1];
                var brand = node.ChildNodes[3];
                var manufacturer = header.ChildNodes.Where(n => n.HasClass("product-card__brand")).First().ChildNodes.ElementAt(1).GetAttributeValue("alt", null).Replace("\n", String.Empty).Trim();
                var name = brand.ChildNodes[3].ChildNodes[1].InnerText.Replace("\n", String.Empty).Trim();
                var model = brand.ChildNodes[5].ChildNodes[1].InnerText.Replace("\n", String.Empty).Trim();
                var price = TextToPrice(brand.ChildNodes[7].ChildNodes[1].InnerText);
                var link = header.ChildNodes.Where(n => n.HasClass("product-card__name")).First().ChildNodes[1].GetAttributeValue("href", null);
                var result = new ScrapedProduct
                {
                    Id = new Guid(),
                    Name = name,
                    Cat = model,
                    CurrentPrice = price,
                    ProductSource = ScrapedProductSource.TMS,
                    ProductLink = link,
                    ProductType = productType,
                    Manufacturer = manufacturer,
                    HighestPrice = price,
                    LowestPrice = price
                };
                return result;
            }
            catch (Exception ex)
            {
                return null;
                _logger.LogInformation($"Failed to get product from node with message: {ex.Message}");
            }
        }

        private double? TextToPrice(string text)
        {
            if (text.Contains("צרו קשר")) return null;
            return double.Parse(Regex.Replace(text, "[^.0-9]", ""));
        }
        private int? TryGetPageCount(StampedHtmlDocument document)
        {
            var paginationNode = document.Document.DocumentNode.SelectSingleNode(PaginationXpath);
            if (paginationNode is null) return null;
            var lastPagination = paginationNode.ChildNodes.Last();
            var lastPaginationLink = lastPagination.FirstChild.GetAttributeValue("href", null);
            int pageCount;
            int.TryParse(lastPagination.FirstChild.GetAttributeValue("href", null).Split("page=").Last(), out pageCount);
            return pageCount;
        }
    }
    public record StampedHtmlDocument
    {
        public HtmlDocument Document { get; init; }
        public DateTime Stamp { get; init; } = DateTime.UtcNow;
        public StampedHtmlDocument(HtmlDocument document)
        {
            Document = document;
        }
    }
    public class KspScraper : IScraper
    {
        public Task<List<ScrapedProduct>> ScrapeProducts(CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }
    }
}
