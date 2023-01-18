using Namotion.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Zapper.Api.Data;
using Zapper.Api.Data.Scrapers.Ksp;
using Zapper.Api.Models;
using static System.Net.WebRequestMethods;

namespace Zapper.Api.Services.Scrapers
{

    public record ScrapeableLink(string ProductType, string Link, string? Xpath = null);
    public class KspScraper : ScraperBase
    {

        private HttpClient client = new HttpClient();
        public KspScraper(ILogger<KspScraper> logger)
        {
            this.logger = logger;
            Source = ScrapedProductSource.KSP;
            targetLinks = new List<ScrapeableLink>()        {
            new(LinkConsts.GPUs, "https://ksp.co.il/m_action/api/category/35..1044?sort=1"),
            new(LinkConsts.CPUs, "https://ksp.co.il/m_action/api/category/1027..2..41?sort=1"),
            new(LinkConsts.CPUs, "https://ksp.co.il/m_action/api/category/1027..2..42?sort=1"),
            new(LinkConsts.RAMs, "https://ksp.co.il/m_action/api/category/1038..392?sort=1"),
            new(LinkConsts.HDDs, "https://ksp.co.il/m_action/api/category/1033..145?sort=1"),
            new(LinkConsts.SSDs, "https://ksp.co.il/m_action/api/category/.1033..152.?sort=1")
        };
        }

        protected override async Task<List<ScrapedProduct>> ScrapeProductAsync(ScrapeableLink productLink, CancellationToken token)
        {
            logger.LogInformation($"Scraping product type: {productLink.ProductType} from: {productLink.Link}");
            int page = 0;
            bool isNextPage = false;
            var result = new List<ScrapedProduct>();
            List<ScrapedProduct> partialProducts;
            do
            {
                page++;
                (partialProducts, isNextPage) = await ScrapePageAsync(productLink, page, token);
                if (partialProducts != null)
                    result.AddRange(partialProducts);
                await Task.Delay(400);

            } while (isNextPage);
            logger.LogInformation($"Scraped: {result.Count} items");
            return result;
        }

        private async Task<(List<ScrapedProduct>, bool)> ScrapePageAsync(ScrapeableLink productLink, int page, CancellationToken token)
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
                        logger.LogInformation($"too many requests, wating {loopCount} seconds");
                        await Task.Delay(loopCount * 1000);
                        if (response.Headers.RetryAfter.Delta > TimeSpan.FromSeconds(10))
                            throw new BlockedScrapeException(response.Headers.RetryAfter.Delta);
                    }
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        break;
                    if (loopCount > 10)
                        break;
                } while (true);
                var json = await response.Content.ReadAsStringAsync();
                var pageResult = JObject.Parse(json);
                var result = await ParseProductsAsync((JArray)pageResult["result"]["items"], productLink, timeStamp);
                return (result, pageResult["result"]["next"] != null);
            }
            catch (BlockedScrapeException e)
            {
                throw e;
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
            var discounts = await GetDiscounts(items.Select(i => i["uin"].Value<int>()));
            foreach (var item in items)
            {
                var uin = item["uin"].Value<int>();
                var crapedProduct = await ParseProduct((JObject)item, productLink, timeStamp);
                double? discount;
                var hasDiscount = discounts.TryGetValue(uin, out discount);
                if (hasDiscount)
                {
                    crapedProduct.LowestPrice = discount;
                    crapedProduct.HighestPrice = discount;
                    crapedProduct.CurrentPrice = discount;
                }
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
                ProductSource = Source,
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

        private async Task<Dictionary<int, double?>> GetDiscounts(IEnumerable<int> ids)
        {
            var result = new Dictionary<int, double?>();
            if (ids.Count() < 1)
                return result;
            string parsesItems = String.Join(',', ids);
            string url = $"https://ksp.co.il/m_action/api/bms/{parsesItems}";
            HttpResponseMessage response;
            int loopCount = 7;
            await Task.Delay(10);
            do
            {
                loopCount++;
                response = await client.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    logger.LogInformation($"too many requests, wating {loopCount} seconds");
                    Console.WriteLine("Retry after: " + response.Headers.RetryAfter);
                    if (response.Headers.RetryAfter.Delta > TimeSpan.FromSeconds(10))
                        throw new BlockedScrapeException(response.Headers.RetryAfter.Delta);
                    await Task.Delay(loopCount * 1000);
                }
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    break;
                if (loopCount > 12)
                    break;
            } while (true);
            if (response is null)
                Debugger.Break();
            var data = await response.Content.ReadJsonAsync<BmsResponse>(new JsonSerializer());
            foreach (var item in data.Result.Values)
                if (item.Discount != null)
                    result.Add((int)item.Uin, item.Discount.Value);
            return result;
        }
    }

    public class BlockedScrapeException : Exception
    {
        public TimeSpan? Delta { get; init; }
        public BlockedScrapeException(TimeSpan? delta) : base($"Scraped was blocked due to too many requests: retry after {delta} seconds")
        {
            this.Delta = delta;
        }
    }


}