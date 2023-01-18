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
using Zapper.Api.Services.Scrapers;
using Newtonsoft.Json.Linq;

namespace Zapper.Api.Services
{
    public class PeriodicScrapeService : BackgroundService
    {
        private readonly ILogger<PeriodicScrapeService> _logger;
        private readonly IServiceScopeFactory _factory;
        private readonly TimeSpan defaultPeriod = TimeSpan.FromHours(8);
        private int _executionCount = 0;
        private readonly AsyncQueue<IEnumerable<ScraperBase>> scrapingQueue = new AsyncQueue<IEnumerable<ScraperBase>>();
        public readonly List<PeriodicScrapingTask> scrapingTasks = new List<PeriodicScrapingTask>();
        private ProductsContext scrapedProductsContext;
        private ImageScraper _imageScraper;
        private ImageSettings ImageSettings;
        private List<ScraperBase> availableScrapers;
        public PeriodicScrapeService(ILogger<PeriodicScrapeService> logger, IServiceScopeFactory factory, IOptions<ImageSettings> options)
        {
            _logger = logger;
            _factory = factory;
            ImageSettings = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await using AsyncServiceScope scope = _factory.CreateAsyncScope();
            scrapedProductsContext = scope.ServiceProvider.GetRequiredService<ProductsContext>();
            _imageScraper = new ImageScraper(scrapedProductsContext, ImageSettings.BasePath);
            availableScrapers = scope.ServiceProvider.GetServices<ScraperBase>().ToList();
            //var oldContext = scope.ServiceProvider.GetRequiredService<OldContext>();
            availableScrapers.ForEach(s =>
            {
                scrapingTasks.Add(new PeriodicScrapingTask(scrapingQueue, s, defaultPeriod));
                Directory.CreateDirectory($"{ImageSettings.BasePath}\\{s.Source}");
            });
            scrapingTasks.Add(new PeriodicScrapingTask(scrapingQueue, _imageScraper, defaultPeriod));
            var runner = Run(scrapedProductsContext, cancellationToken);
            await runner;
        }
        //private async Task FakeData(ProductsContext context)
        //{

        //    var products = await context.ScrapedProducts.ToListAsync();
        //    var changes = context.ProductPriceChanges;
        //    var temp = products.Take(40);
        //    foreach (var item in temp)
        //    {
        //        var c1 = new ProductPriceChange
        //        {
        //            Changed = DateTime.UtcNow,
        //            Id = Guid.NewGuid(),
        //            CurrentPrice = 999,
        //            PreviousPrice = 888,
        //            Product = item
        //        };
        //        var c2 = new ProductPriceChange
        //        {
        //            Changed = DateTime.UtcNow,
        //            Id = Guid.NewGuid(),
        //            CurrentPrice = 888,
        //            PreviousPrice = 777,
        //            Product = item
        //        };
        //        //item.Changes.Add(c1);
        //        //item.Changes.Add(c2);
        //        changes.Add(c1);
        //        changes.Add(c2);
        //        //context.Entry(c1).State = EntityState.Added;
        //        //context.Entry(c2).State = EntityState.Added;
        //    }
        //    await context.SaveChangesAsync();
        //}
        private async Task FakeData(ProductsContext context)
        {
            var changes = new List<ProductPriceChange>();
            for (int i = 0; i < 3; i++)
            {
                var change = new ProductPriceChange
                {
                    CurrentPrice = 999,
                    PreviousPrice = 1200,
                    Changed = DateTime.UtcNow,
                    Id = Guid.NewGuid(),
                };
                changes.Add(change);
            }
            var product = new ScrapedProduct()
            {
                Id = Guid.NewGuid(),
                Cat = "1111",
                CreationDate = DateTime.UtcNow,
                LastChanged = DateTime.UtcNow,
                LastChecked = DateTime.UtcNow,
                HighestPrice = 9999,
                LowestPrice = 8888,
                CurrentPrice = 9500,
                ImageUri = new Uri("http://192.168.1.246:8080/?pgsql=db&username=Admin&db=ZapperDB&ns=public&select=ScrapedProducts"),
                Name = "My Product",
                ProductSource = ScrapedProductSource.Bug,
                ProductLink = "",
                ProductType = "My Type",
                HasImage = false,

            };
            product.Changes.AddRange(changes);
            context.ScrapedProducts.Add(product);
            await context.SaveChangesAsync();
        }
        private async Task PopulateImagePath()
        {
            var products = await scrapedProductsContext.ScrapedProducts.ToListAsync();
            var dir = new DirectoryInfo("V:\\Data\\Services\\Zapper\\Thumbnails");
            int counter = 1;
            var items = products.Where(p => p.HasImage && string.IsNullOrEmpty(p.ImagePath));
            foreach (var product in items)
            {
                var sourceDir = dir.GetDirectories().Where(d => d.Name == product.ProductSource.ToString()).First();
                var image = sourceDir.GetFiles().FirstOrDefault(f => f.Name.Contains(product.Id.ToString()));
                if (image != null)
                {
                    var ext = image.Extension;
                    product.ImagePath = $"{product.ProductSource}/{product.Id}{ext}";
                    Console.WriteLine($"{counter}# - Added path to: {product.ImagePath}\r");
                }
                else
                {
                    Console.WriteLine($"\nFailed to get: {product}");
                }

                counter++;
            }
            Console.WriteLine();
            scrapedProductsContext.SaveChanges();

        }

        private async Task FixMissingChanges(ProductsContext newContext, OldContext oldContext)
        {
            var originalProducts = oldContext.ScrapedProducts;
            var originalChanges = oldContext.ProductPriceChanges.ToList();
            var products = newContext.ScrapedProducts;
            //var newPriceChanges = newContext.ProductPriceChanges;
            Console.WriteLine($"Fixing missing items, total products: {originalProducts.Count()}");
            int fixCount = 0;
            foreach (var originalProduct in originalProducts)
            {
                var changes = originalChanges.Where(c => c.ProductId == originalProduct.Id);
                var newProduct = new ScrapedProduct
                {
                    Cat = originalProduct.Cat,
                    Name = originalProduct.Name,
                    CreationDate = originalProduct.CreationDate,
                    LastChanged = originalProduct.LastChanged,
                    LastChecked = originalProduct.LastChecked,
                    Id = originalProduct.Id,
                    CurrentPrice = originalProduct.CurrentPrice,
                    HighestPrice = originalProduct.HighestPrice,
                    LowestPrice = originalProduct.LowestPrice,
                    ImagePath = originalProduct.ImagePath,
                    ImageUri = originalProduct.ImageUri,
                    Manufacturer = originalProduct.Manufacturer,
                    ManufacturerSerial = originalProduct.ManufacturerSerial,
                    ProductLink = originalProduct.ProductLink,
                    ProductSource = originalProduct.ProductSource,
                    ProductType = originalProduct.ProductType,
                    HasImage = originalProduct.HasImage

                };
                products.Add(newProduct);
                Console.WriteLine($"Adding {changes.Count()} changes to: {originalProduct.Name,40}\r");
                foreach (var oldChange in changes)
                {
                    var newChange = new ProductPriceChange
                    {
                        Changed = oldChange.Changed,
                        Product = newProduct,
                        Id = oldChange.Id,
                        CurrentPrice = oldChange.CurrentPrice,
                        PreviousPrice = oldChange.PreviousPrice
                    };
                    //newContext.Entry(newChange).State = EntityState.Added;
                    newProduct.Changes.Add(newChange);
                }
                fixCount++;
            }
            Console.WriteLine();
            Console.WriteLine($"Fixed {fixCount} changes");
            await scrapedProductsContext.SaveChangesAsync();

        }

        private Task Run(ProductsContext scrapedProductsContext, CancellationToken cancellationToken)
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
                    Debug.WriteLine($"Finished task excecution number: {_executionCount}#, time: {DateTime.Now}");
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
            _logger.LogInformation("\nStarting scraping round:");
            List<Task<List<ScrapedProduct>>> scrapingTasks = new List<Task<List<ScrapedProduct>>>();
            foreach (var scraper in scrapers)
                scrapingTasks.Add(scraper.ScrapeProductsAsync(stoppingToken));
            await Task.WhenAll(scrapingTasks);
            var result = new List<ScrapedProduct>();
            scrapingTasks.ForEach(t => result.AddRange(t.Result));
            _logger.LogInformation($"Finished scraping in {watch.Elapsed} - scrape number: {_executionCount}#");
            return result;
        }
        private async Task<(List<ScrapedProduct>, List<ProductPriceChange>)> UpdateDataAsync(List<ScrapedProduct> scrapedProducts, ProductsContext scrapedProductsContext, CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var currentProducts = await scrapedProductsContext.ScrapedProducts.Include(p => p.Changes).ToListAsync();
            var newProducts = new List<ScrapedProduct>();
            var newChanges = new List<ProductPriceChange>();
            await Task.Run(async () =>
            {
                foreach (var scrapedProduct in scrapedProducts)
                {
                    ScrapedProduct current;
                    var matches = currentProducts.Where(p => p.ProductSource == scrapedProduct.ProductSource && p.Cat == scrapedProduct.Cat);
                    if (matches.Count() > 1)
                    {
                        //collision - skip:

                        //var temp = await ResolveProductCollision(matches.ToList(), scrapedProduct);
                    }
                    else if (matches.Count() == 0)
                    {
                        newProducts.Add(scrapedProduct);
                        _imageScraper.ImageQueue.Enqueue(scrapedProduct);
                    }
                    else
                    {
                        current = matches.First();
                        if (current.CurrentPrice != scrapedProduct.CurrentPrice)
                        {
                            var change = GetChange(current, scrapedProduct);
                            newChanges.Add(change);
                            current.Changes.Add(change);
                        }
                        current.LastChecked = scrapedProduct.LastChecked;

                    }
                }
                await _imageScraper.ScrapeQueueParallelAsync(_imageScraper.ImageQueue, cancellationToken);
            });
            await scrapedProductsContext.ScrapedProducts.AddRangeAsync(newProducts);
            await scrapedProductsContext.ProductPriceChanges.AddRangeAsync(newChanges);
            await scrapedProductsContext.SaveChangesAsync();
            stopwatch.Stop();
            _logger.LogInformation($"Updated scraped data in {stopwatch.Elapsed}");
            _logger.LogInformation($"New Products: {newProducts.Count}, new Changes: {newChanges.Count}");
            return (newProducts, newChanges);
        }
        private ProductPriceChange GetChange(ScrapedProduct current, ScrapedProduct scrapedProduct)
        {
            var result = new ProductPriceChange
            {
                Changed = scrapedProduct.LastChanged,
                CurrentPrice = scrapedProduct.CurrentPrice,
                PreviousPrice = current.CurrentPrice,
                Id = Guid.NewGuid(),
                Product = current
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
        private async Task<ScrapedProduct> ResolveProductCollision(List<ScrapedProduct> currentProducts, ScrapedProduct scrapedProduct)
        {
            var merged = currentProducts[0];
            for (int i = 1; i < currentProducts.Count; i++)
            {
                var changes = currentProducts[i].Changes;
                changes.ForEach(c => c.Id = merged.Id);
                merged.Changes.AddRange(changes);
            }
            var first= currentProducts.FirstOrDefault(cp => cp.CurrentPrice != scrapedProduct.CurrentPrice);
            if(first != null)
            {
                return merged;
            }
            return first;
        }


        public IEnumerable<ScrapedProductSource> GetPeriodicScrapersSources()
        {
            return availableScrapers.Select(s => s.Source);
        }
        public void ChangeScraperPeriod(ScrapedProductSource source, TimeSpan newPeriodTimeSpan)
        {
            var scrapingTask = scrapingTasks.First(p => p.Source == source);
            scrapingTask.UpdatePeriod(newPeriodTimeSpan);
        }
        public void AddScrapingToQueue(ScrapedProductSource source)
        {
            var scrapingTask = scrapingTasks.FirstOrDefault(p => p.Source == source);
            if (scrapingTask != null)
                scrapingQueue.Enqueue(new[] { scrapingTask.Scraper });
        }
        public TimeSpan GetRemainingTime(ScrapedProductSource source)
        {
            var scrapingTask = scrapingTasks.First(p => p.Source == source);
            return scrapingTask.RemainingTime();
        }

        public async Task ResolveMissingImagesAsync(IEnumerable<ScraperBase> scrapers, IEnumerable<ScrapedProduct> scrapedProducts)
        {
            foreach (var scraper in scrapers)
            {
                var products = scrapedProducts.Where(p => p.ProductSource == scraper.Source);
                await scraper.ResolveMissingImagesAsync(products);
            }
        }

    }

    public class ImageScraper : ScraperBase, IDisposable
    {
        private int parallelLimit = 10;
        private string BasePath;
        public readonly AsyncQueue<ScrapedProduct> ImageQueue = new AsyncQueue<ScrapedProduct>();
        //private readonly AsyncQueue<ScrapedProduct> failedImageQueue = new AsyncQueue<ScrapedProduct>();
        HttpClient client = new HttpClient() { };
        private ProductsContext scrapedProductsContext;
        public new ScrapedProductSource Source { get; protected set; } = ScrapedProductSource.Images;
        public ImageScraper(ProductsContext scrapedProductsContext, string basePath)
        {
            this.scrapedProductsContext = scrapedProductsContext;
            BasePath = basePath;
            logger = new Logger<ImageScraper>(LoggerFactory.Create(_ => {; }));
            targetLinks = new List<ScrapeableLink> { new ScrapeableLink("Images", "") };
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

        protected override async Task<List<ScrapedProduct>> ScrapeProductAsync(ScrapeableLink link, CancellationToken token)
        {
            var missingImages = scrapedProductsContext.ScrapedProducts.Where(p => p.HasImage == false);
            if (missingImages.Count() > 0)
            {
                await missingImages.ForEachAsync(p => ImageQueue.Enqueue(p));
                int failures = await ScrapeQueueParallelAsync(ImageQueue, token);
            }
            return new List<ScrapedProduct>();

        }

        public async Task<int> ScrapeQueueParallelAsync(AsyncQueue<ScrapedProduct> queue, CancellationToken token)
        {
            var tasks = new List<Task>();
            int loopCounter = 0;
            int failCounter = 0;
            if (ImageQueue.Count == 0)
                return failCounter;
            Console.WriteLine($"\nScraping Images:\n {ImageQueue.Count} in queue\n\n");
            using (SemaphoreSlim semaphore = new SemaphoreSlim(parallelLimit))
            {
                while (!queue.IsEmpty && !token.IsCancellationRequested)
                {
                    loopCounter++;
                    var scrapedProduct = await queue.DequeueAsync(token);
                    await semaphore.WaitAsync(token);
                    Console.Write($"Failed: {failCounter} Downloading image {loopCounter}# from: {scrapedProduct.ImageUri}\r");
                    var currentTask = TryDownloadImage(scrapedProduct, token);
                    currentTask.ContinueWith(t =>
                    {
                        semaphore.Release();
                        if (!t.Result)
                            failCounter++;
                    }, token);
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

        private async Task ScrapeInOrderAsync(CancellationToken cancellationToken)
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
    }
}
