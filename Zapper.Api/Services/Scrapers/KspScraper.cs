using Namotion.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Zapper.Api.Data;
using Zapper.Api.Models;
using static System.Net.WebRequestMethods;

namespace Zapper.Api.Services.Scrapers
{

    public record ScrapeableLink(string ProductType, string Link);
    public class KspScraper : IScraper
    {
        private readonly IEnumerable<ScrapeableLink> targetProducts = new List<ScrapeableLink>()        {
            new ("Graphics Cards", "https://ksp.co.il/m_action/api/category/35..1044?sort=1"),
            new("Processors", "https://ksp.co.il/m_action/api/category/1027..2..41?sort=1"),
            new("Processors", "https://ksp.co.il/m_action/api/category/1027..2..42?sort=1"),
            new("Random Access Memory", "https://ksp.co.il/m_action/api/category/1038..392?sort=1"),
            new("HDD", "https://ksp.co.il/m_action/api/category/1033..145?sort=1"),
            new("SSD", "https://ksp.co.il/m_action/api/category/.1033..152.?sort=1")
        };

        private readonly ILogger<TmsScraper> _logger;
        private readonly ScrapedProductSource source = ScrapedProductSource.KSP;
        private HttpClient client = new HttpClient();
        //private JsonConverter JsonConverter = new JsonConverter();
        public KspScraper(ILogger<TmsScraper> logger)
        {
            _logger = logger;
        }

        public ScrapedProductSource GetSource() => source;

        public async Task<List<ScrapedProduct>> ScrapeProducts(CancellationToken stoppingToken)
        {
            List<ScrapedProduct> result = new();
            foreach (var product in targetProducts)
            {
                var partialProducts = await ScrapeProductAsync(product);
                result.AddRange(partialProducts);
            }
            return result;
        }

        private async Task<List<ScrapedProduct>> ScrapeProductAsync(ScrapeableLink productLink)
        {
            _logger.LogInformation($"Scraping product type: {productLink.ProductType} from: {productLink.Link}");
            int page = 0;
            bool isNextPage = false;
            var result = new List<ScrapedProduct>();
            List<ScrapedProduct> partialProducts;
            do
            {
                page++;
                (partialProducts, isNextPage) = await ScrapePageAsync(productLink, page);
                if (partialProducts != null)
                    result.AddRange(partialProducts);

            } while (isNextPage);
            _logger.LogInformation($"Scraped: {result.Count} items");
            return result;
        }

        private async Task<(List<ScrapedProduct>, bool)> ScrapePageAsync(ScrapeableLink productLink, int page)
        {

            string url = $"{productLink.Link}&page={page}";
            var timeStamp = DateTime.UtcNow;
            HttpResponseMessage response;
            try
            {
                int loopCount = 5;
                await Task.Delay(10);
                do
                {
                    loopCount++;
                    response = await client.GetAsync(url);
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        _logger.LogInformation($"too many requests, wating {loopCount} seconds");
                        await Task.Delay(loopCount * 1000);
                    }
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        break;
                    if (loopCount > 8)
                        break;
                } while (true);

                var json = await response.Content.ReadAsStringAsync();
                var pageResult = JObject.Parse(json);
                var result = await ParseProductsAsync((JArray)pageResult["result"]["items"], productLink, timeStamp);
                return (result, pageResult["result"]["next"] != null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get page: {ex}");
                return (null, false);
            }
        }

        private async Task<List<ScrapedProduct>> ParseProductsAsync(JArray items, ScrapeableLink productLink, DateTime timeStamp)
        {
            var result = new List<ScrapedProduct>();
            foreach (var item in items)
            {
                var crapedProduct = await ParseProduct((JObject)item, productLink, timeStamp);
                result.Add(crapedProduct);
            }
            return result;
        }

        private async Task<ScrapedProduct> ParseProduct(JObject item, ScrapeableLink productLink, DateTime dateStamp)
        {
            var result = new ScrapedProduct
            {
                Id = Guid.NewGuid(),
                ImageUri = new Uri(item["img"].Value<string>()),
                LowestPrice = item["price"].Value<double>(),
                CurrentPrice = item["price"].Value<double>(),
                HighestPrice = item["price"].Value<double>(),
                Name = item["name"].Value<string>(),
                ProductSource = source,
                Cat = item["uinsql"].Value<string>(),
                Manufacturer = item["brandName"].Value<string>(),
                ProductType = productLink.ProductType,
                CreationDate = dateStamp,
                LastChanged = dateStamp,
                LastChecked = dateStamp,
                ProductLink = $"https://ksp.co.il/web/item/{item["uin"].Value<string>()}"
            };
            return result;
        }

        public Task ResolveMissingImagesAsync(IEnumerable<ScrapedProduct> scrapedProducts)
        {
            throw new NotImplementedException();
        }

    }
}