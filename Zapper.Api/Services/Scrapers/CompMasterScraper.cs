using HtmlAgilityPack;
using System.Text.RegularExpressions;
using Zapper.Api.Models;

namespace Zapper.Api.Services.Scrapers
{
    public class CompMasterScraper : ScraperBase
    {
        private HttpClient client = new HttpClient();
        public CompMasterScraper(ILogger<CompMasterScraper> logger)
        {
            this.logger = logger;
            Source = ScrapedProductSource.CompMaster;
            targetLinks = new List<ScrapeableLink>() {
            new(LinkConsts.GPUs, "https://comp-shop.co.il/רכיבי-חומרה-ותוכנה/כרטיסי-מסך/","/html/body/div[4]/div/div/div/div[3]/div[2]"),
            new(LinkConsts.CPUs, "https://comp-shop.co.il/רכיבי-חומרה-ותוכנה/מעבדים/","/html/body/div[4]/div/div/div/div[3]/div[2]"),
            new(LinkConsts.RAMs, "https://comp-shop.co.il/רכיבי-חומרה-ותוכנה/זכרונות/זכרונות-לנייחים/","/html/body/div[4]/div/div/div/div[2]/div[2]"),
            new(LinkConsts.SSDs, "https://comp-shop.co.il/רכיבי-חומרה-ותוכנה/אחסון/דיסקים-ssd/","/html/body/div[4]/div/div/div/div[2]/div[2]"),
            new(LinkConsts.HDDs, "https://comp-shop.co.il/רכיבי-חומרה-ותוכנה/אחסון/דיסקים-קשיחים/","/html/body/div[4]/div/div/div/div[2]/div[2]"),
        };
        }


        //public override async Task<List<ScrapedProduct>> ScrapeProductsAsync(CancellationToken token)
        //{
        //    List<ScrapedProduct> result = new List<ScrapedProduct>();
        //    IsRunning = true;
        //    try
        //    {
        //        foreach (var link in targetLinks)
        //        {
        //            var partialProducts = await ScrapeProductAsync(link, token);
        //            result.AddRange(partialProducts);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogError($"Failed to scrape from: {Source}", ex);
        //    }
        //    finally
        //    {
        //        IsRunning = false;
        //    }
        //    return result;
        //}

        protected override async Task<List<ScrapedProduct>> ScrapeProductAsync(ScrapeableLink link, CancellationToken token)
        {
            string url = $"{link.Link}?limit=999999";
            var response = await client.GetAsync(url);
            var products = new List<ScrapedProduct>();
            HtmlDocument doc = new HtmlDocument();
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var stream = await response.Content.ReadAsStreamAsync();
                doc.Load(stream);
                products = await ScrapePageAsync(doc, link, DateTime.UtcNow, token);
            }
            else
            {
                Console.WriteLine("Failed to get OK response");
            }
            return products;
        }

        protected async Task<List<ScrapedProduct>> ScrapePageAsync(HtmlDocument document, ScrapeableLink link, DateTime dateStamp, CancellationToken token)
        {
            string itemsXpath = link.Xpath;
            var productsNode = document.DocumentNode.SelectSingleNode(itemsXpath);
            var products = new List<ScrapedProduct>();
            foreach (var node in productsNode.ChildNodes)
            {
                var product = await ParseProductAsync(node, link, dateStamp, token);
                products.Add(product);
            }
            return products;
        }

        protected async Task<ScrapedProduct> ParseProductAsync(HtmlNode node, ScrapeableLink link, DateTime dateStamp, CancellationToken token)
        {
            var imagePath = $"{node.XPath}/div/div[1]/a/div/img";
            var namePath = $"{node.XPath}/div/div[2]/div[2]/a";
            var pricePath = $"{node.XPath}/div/div[2]/div[4]/span";
            double price = node.SelectSingleNode(pricePath).InnerHtml.AsPrice();
            if (node.SelectSingleNode($"{node.XPath}/div/div[2]/div[4]").ChildNodes.Where(c => c.HasClass("price-new")).Count() > 0)
                price = node.SelectSingleNode($"{node.XPath}/div/div[2]/div[4]/span[2]").InnerHtml.AsPrice();
            var catPath = $"{node.XPath}/div/div[2]/div[1]/span[1]/span[2]";
            var linkPath = $"{node.XPath}/div/div[1]/a";
            var serialPath = $"{node.XPath}/div/div[2]/div[1]/span[2]/span[2]";
            var product = new ScrapedProduct
            {
                Id = Guid.NewGuid(),
                ImageUri = new Uri(node.SelectSingleNode(imagePath).GetAttributeValue("data-src", null)),
                Name = node.SelectSingleNode(namePath).InnerHtml,
                HighestPrice = price,
                CurrentPrice = price,
                LowestPrice = price,
                Cat = node.SelectSingleNode(catPath).InnerHtml,
                ProductLink = node.SelectSingleNode(linkPath).GetAttributeValue("href", null),
                ProductSource = Source,
                ProductType = link.ProductType,
                CreationDate = dateStamp,
                LastChanged = dateStamp,
                LastChecked = dateStamp,
                ManufacturerSerial = node.SelectSingleNode(serialPath).InnerHtml,
            };
            return product;
        }
    }

    public static class StringEx
    {
        public static double AsPrice(this string text)
        {
            return double.Parse(Regex.Replace(text, "[^.0-9]", ""));
        }
    }
}
