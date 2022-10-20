using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Threading;
using Npgsql.Replication.PgOutput.Messages;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using YamlDotNet.Core.Tokens;
using Zapper.Api.Data;
using Zapper.Api.Models;
using System.Net.Http;
using System.Threading;
using System.IO;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Options;

namespace Zapper.Api.Services
{
    public class PeriodicScrapeService : BackgroundService
    {
        private readonly ILogger<PeriodicScrapeService> _logger;
        private readonly IServiceScopeFactory _factory;
        private readonly TimeSpan defaultPeriod = TimeSpan.FromHours(8);
        private int _executionCount = 0;
        private readonly AsyncQueue<IEnumerable<IScraper>> scrapingQueue = new AsyncQueue<IEnumerable<IScraper>>();
        private readonly List<PeriodicScrapingTask> scrapingTasks = new List<PeriodicScrapingTask>();
        private ScrapedProductsContext scrapedProductsContext;
        private ImageScraper _imageScraper;
        private ImageSettings ImageSettings;
        public PeriodicScrapeService(ILogger<PeriodicScrapeService> logger, IServiceScopeFactory factory, IOptions<ImageSettings> options)
        {
            _logger = logger;
            _factory = factory;
            ImageSettings = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await using AsyncServiceScope asyncScope = _factory.CreateAsyncScope();
            scrapedProductsContext = asyncScope.ServiceProvider.GetRequiredService<ScrapedProductsContext>();
            _imageScraper = new ImageScraper(scrapedProductsContext, ImageSettings.BasePath);
            List<IScraper> availableScrapers = asyncScope.ServiceProvider.GetServices<IScraper>().ToList();
            availableScrapers.ForEach(s =>
            {
                scrapingTasks.Add(new PeriodicScrapingTask(scrapingQueue, s, defaultPeriod));
                Directory.CreateDirectory($"{ImageSettings.BasePath}\\{s.GetSource()}");
            });
            scrapingTasks.Add(new PeriodicScrapingTask(scrapingQueue, _imageScraper, defaultPeriod));
            var runner = Run(scrapedProductsContext, cancellationToken);
            await runner;
        }

        private Task Run(ScrapedProductsContext scrapedProductsContext, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                IEnumerable<IScraper> scrapers;
                do
                {
                    (List<ScrapedProduct> newProducts, List<ProductPriceChange> priceChanges) result = (null, null);
                    scrapers = await scrapingQueue.DequeueAsync(cancellationToken);
                    if (cancellationToken.IsCancellationRequested) break;
                    try
                    {
                        var scrapedProducts = await ScrapeAllAsync(scrapers, cancellationToken);
                        result = await UpdateDataAsync(scrapedProducts, scrapedProductsContext, cancellationToken);

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to execute {nameof(PeriodicScrapeService)} with exception:\n{ex}");
                    }
                    _executionCount++;
                    //if (result.newProducts != null)
                    //{
                    //    await ResolveMissingImagesAsync(scrapers, result.newProducts.Where(p => !p.HasImage));
                    //    //await scrapedProductsContext.SaveChangesAsync();
                    //}


                } while (true);
            });
        }
        private async Task<List<ScrapedProduct>> ScrapeAllAsync(IEnumerable<IScraper> scrapers, CancellationToken stoppingToken)
        {
            Stopwatch watch = Stopwatch.StartNew();
            _logger.LogInformation("Starting scraping round:");
            List<Task<List<ScrapedProduct>>> scrapingTasks = new List<Task<List<ScrapedProduct>>>();
            foreach (var scraper in scrapers)
                scrapingTasks.Add(scraper.ScrapeProducts(stoppingToken));
            await Task.WhenAll(scrapingTasks);
            var result = new List<ScrapedProduct>();
            scrapingTasks.ForEach(t => result.AddRange(t.Result));
            _logger.LogInformation($"Finished scraping successfuly in {watch.Elapsed} - scrape number: {_executionCount}#");
            return result;
        }
        private async Task<(List<ScrapedProduct>, List<ProductPriceChange>)> UpdateDataAsync(List<ScrapedProduct> scrapedProducts, ScrapedProductsContext scrapedProductsContext, CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var currentProducts = await scrapedProductsContext.ScrapedProducts.ToListAsync();
            var newProducts = new List<ScrapedProduct>();
            var newChanges = new List<ProductPriceChange>();
            await Task.Run(async () =>
            {
                foreach (var scrapedProduct in scrapedProducts)
                {
                    var current = currentProducts.SingleOrDefault(p => p.ProductSource == scrapedProduct.ProductSource && p.Cat == scrapedProduct.Cat);
                    if (current == null)
                    {
                        newProducts.Add(scrapedProduct);
                        _imageScraper.ImageQueue.Enqueue(scrapedProduct);
                    }
                    else
                    {
                        if (current.CurrentPrice != scrapedProduct.CurrentPrice)
                            newChanges.Add(GetChange(current, scrapedProduct));
                        current.LastChecked = scrapedProduct.LastChecked;
                    }
                }
                await _imageScraper.ScrapeParallelAsync(cancellationToken);
            });
            await scrapedProductsContext.ScrapedProducts.AddRangeAsync(newProducts);
            await scrapedProductsContext.ProductPriceChanges.AddRangeAsync(newChanges);
            await scrapedProductsContext.SaveChangesAsync();
            stopwatch.Stop();
            _logger.LogInformation($"Updated scraped data in {stopwatch.Elapsed}");
            return (newProducts, newChanges);
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

        public IEnumerable<ScrapedProductSource> GetPeriodicScrapersSources()
        {
            return scrapingTasks.Select(p => p.GetScraperSource());
        }
        public void ChangeScraperPeriod(ScrapedProductSource source, TimeSpan newPeriodTimeSpan)
        {
            var scrapingTask = scrapingTasks.First(p => p.GetScraperSource() == source);
            scrapingTask.UpdatePeriod(newPeriodTimeSpan);
        }
        public void AddScrapingToQueue(ScrapedProductSource source)
        {
            var scrapingTask = scrapingTasks.First(p => p.GetScraperSource() == source);
            scrapingQueue.Enqueue(new[] { scrapingTask.Scraper });
        }
        public TimeSpan GetRemainingTime(ScrapedProductSource source)
        {
            var scrapingTask = scrapingTasks.First(p => p.GetScraperSource() == source);
            return scrapingTask.RemainingTime();
        }

        public async Task ResolveMissingImagesAsync(IEnumerable<IScraper> scrapers, IEnumerable<ScrapedProduct> scrapedProducts)
        {
            foreach (var scraper in scrapers)
            {
                var products = scrapedProducts.Where(p => p.ProductSource == scraper.GetSource());
                await scraper.ResolveMissingImagesAsync(products);
            }
        }
    }

    public class ImageScraper : IDisposable, IimageScraper
    {
        private int parallelLimit = 10;
        private string BasePath;
        public readonly AsyncQueue<ScrapedProduct> ImageQueue = new AsyncQueue<ScrapedProduct>();
        //private readonly AsyncQueue<ScrapedProduct> failedImageQueue = new AsyncQueue<ScrapedProduct>();
        HttpClient client = new HttpClient() { };
        private ScrapedProductsContext scrapedProductsContext;
        public ImageScraper(ScrapedProductsContext scrapedProductsContext, string basePath)
        {
            this.scrapedProductsContext = scrapedProductsContext;
            BasePath = basePath;
        }

        //public ImageScraper(List<ScrapedProduct> products, int parallelLimit = 10)
        //{
        //    products.ForEach(p => ImageQueue.Enqueue(p));
        //    this.parallelLimit = parallelLimit;
        //}

        public async Task ScrapeAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            int failCount = 0, count = 0;
            Console.WriteLine("Scraping Images:");
            while (!ImageQueue.IsEmpty)
            {
                count++;
                var scrapedProduct = await ImageQueue.DequeueAsync(cancellationToken);
                Console.Write($"Downloading image {count}# from: {scrapedProduct.ImageUri}\r");
                string path = $"{BasePath}\\{scrapedProduct.ProductSource}\\{scrapedProduct.Id}.jpg";
                var response = await client.GetAsync(scrapedProduct.ImageUri);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    response = await client.GetAsync(scrapedProduct.ImageUri);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var bytes = await response.Content.ReadAsByteArrayAsync();
                        await File.WriteAllBytesAsync(path, bytes);
                        scrapedProduct.HasImage = true;
                    }
                    else
                        //try
                        //{

                        //    var wc = new WebClient();
                        //    await wc.download(scrapedProduct.ImageUri, path);
                        //    var file = new FileInfo(path);
                        //    if (file.Length < 1024)
                        //        file.Delete();
                        //    else
                        //        scrapedProduct.HasImage = true;
                        //}
                        //catch (Exception e)
                        //{
                        //    failCount++;
                        //}

                        failCount++;
                }
                else
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(path, bytes);
                    scrapedProduct.HasImage = true;
                }
            }
            Console.WriteLine($"\nFinished Downloading with {failCount} failures");
        }

        public async Task ScrapeParallelAsync(CancellationToken cancellationToken)
        {
            int failures = await ScrapeQueueParallelAsync(ImageQueue, cancellationToken);
        }

        public async Task<int> ScrapeQueueParallelAsync(AsyncQueue<ScrapedProduct> queue, CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();
            Console.WriteLine($"\nScraping Images:\n");
            int loopCounter = 0;
            int failCounter = 0;
            using (SemaphoreSlim semaphore = new SemaphoreSlim(parallelLimit))
            {
                while (!queue.IsEmpty && !cancellationToken.IsCancellationRequested)
                {
                    loopCounter++;
                    var scrapedProduct = await queue.DequeueAsync(cancellationToken);
                    await semaphore.WaitAsync(cancellationToken);
                    Console.Write($"Failed: {failCounter} Downloading image {loopCounter}# from: {scrapedProduct.ImageUri}\r");
                    var currentTask = TryDownloadImage(scrapedProduct, cancellationToken);
                    currentTask.ContinueWith(t =>
                    {
                        semaphore.Release();
                        if (!t.Result)
                            failCounter++;
                    }, cancellationToken);
                    tasks.Add(currentTask);
                }
                await Task.WhenAll(tasks);
                Console.WriteLine($"failed: {failCounter} out of {loopCounter}\n");
            }
            return failCounter;
        }

        private async Task<bool> TryDownloadImage(ScrapedProduct scrapedProduct, CancellationToken cancellationToken)
        {

            var response = await client.GetAsync(scrapedProduct.ImageUri);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return false;
            }
            else
            {
                string ext = scrapedProduct.ImageUri.AbsoluteUri.Split('.').Last().Split('?').First();
                string path = $"{BasePath}\\{scrapedProduct.ProductSource}\\{scrapedProduct.Id}.{ext}";
                var bytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(path, bytes);
                scrapedProduct.HasImage = true;
            }
            return true;
        }

        public async Task ScrapeInOrderAsync(CancellationToken cancellationToken)
        {
            int failCount = 0;
            while (!ImageQueue.IsEmpty && !cancellationToken.IsCancellationRequested)
            {
                var scrapedProduct = await ImageQueue.DequeueAsync();
                var response = await client.GetAsync(scrapedProduct.ImageUri);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine($"Failed to get image from: {scrapedProduct.ImageUri}");
                    //ImageQueue.Enqueue(scrapedProduct);
                    failCount++;
                    continue;
                }
                string ext = scrapedProduct.ImageUri.AbsoluteUri.Split('.').Last();
                string path = $"{BasePath}\\{scrapedProduct.ProductSource}\\{scrapedProduct.Id}.{ext}";
                var bytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(path, bytes);
                scrapedProduct.HasImage = true;
                Console.WriteLine($"{ImageQueue.Count} left in queue, Image saved to: {path}");
            }
            Console.WriteLine($"Finished downloading images, failed: {failCount}");
        }

        public void Dispose()
        {
            client.Dispose();
        }

        public async Task<List<ScrapedProduct>> ScrapeProducts(CancellationToken stoppingToken)
        {
            await scrapedProductsContext.ScrapedProducts.Where(p => !p.HasImage).ForEachAsync(p => ImageQueue.Enqueue(p));
            //await ScrapeParallelAsync(stoppingToken);
            return new List<ScrapedProduct>();
        }

        public ScrapedProductSource GetSource() => ScrapedProductSource.Images;

        public Task ResolveMissingImagesAsync(IEnumerable<ScrapedProduct> scrapedProducts)
        {
            throw new NotImplementedException();
        }
    }
}
