﻿using HtmlAgilityPack;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using Zapper.Api.Data;
using Zapper.Api.Models;

namespace Zapper.Api.Services.Scrapers
{

    public class TmsScraper : ScraperBase
    {
        private readonly string PaginationXpath = "/html/body/div[1]/div/div/div[2]/div[6]/div[1]/ul";
        private readonly string ProducstsXpath = "/html/body/div[1]/div/div/div[2]/div[5]";
        public TmsScraper(ILogger<TmsScraper> logger)
        {
            this.logger = logger;
            Source = ScrapedProductSource.TMS;
            targetLinks = new List<ScrapeableLink> {
         new(LinkConsts.GPUs,"https://tms.co.il/computer-hardware-components/video-cards?limit=100"),
         new(LinkConsts.CPUs,"https://tms.co.il/computer-hardware-components/prossesor?limit=100"),
         new(LinkConsts.RAMs,"https://tms.co.il/computer-hardware-components/memory?limit=100"),
         new(LinkConsts.HDDs,"https://tms.co.il/computer-hardware-components/hard-drives?limit=100"),
         new(LinkConsts.SSDs,"https://tms.co.il/computer-hardware-components/ssd-drives?limit=100")
        };
        }

        //public override async Task<List<ScrapedProduct>> ScrapeProductsAsync(CancellationToken stoppingToken)
        //{
        //    IsRunning = true;
        //    logger.LogInformation($"Starting product scrape");
        //    List<Task<List<ScrapedProduct>>> scrapingTasks = new List<Task<List<ScrapedProduct>>>();
        //    List<ScrapedProduct> scrapedProducts = new List<ScrapedProduct>();
        //    try
        //    {
        //        foreach (var product in targetLinks)
        //            scrapingTasks.Add(ScrapeProdcutType(product));
        //        await Task.WhenAll(scrapingTasks);
        //        foreach (var t in scrapingTasks)
        //            scrapedProducts.AddRange(t.Result);
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogError($"Failed to scrape from: {Source}", ex);
        //    }
        //    finally
        //    {
        //        IsRunning = false;
        //    }
        //    return scrapedProducts;
        //}

        protected override async Task<List<ScrapedProduct>> ScrapeProductAsync(ScrapeableLink productLink, CancellationToken token)
        {
            HtmlWeb web = new HtmlWeb();
            StampedHtmlDocument stampedDocument = null;
            List<ScrapedProduct> scrapedProducts = new List<ScrapedProduct>();
            List<List<ScrapedProduct>> scrapedResults = new List<List<ScrapedProduct>>();
            try
            {
                await Task.Run(() => stampedDocument = new(web.Load(productLink.Link)));
                var result = await ScrapePage(stampedDocument, productLink);
                scrapedResults.Add(result);
                scrapedProducts.AddRange(result);
                int? pageCount = TryGetPageCount(stampedDocument);
                if (pageCount is null)
                    return result;
                for (int page = 2; page <= pageCount; page++)
                {
                    result = await ScrapePage(page, productLink);
                    scrapedResults.Add(result);
                    scrapedProducts.AddRange(result);
                }
                //logger.LogInformation($"Scraped {scrapedProducts.Count} items of prodcut type: {productLink.ProductType} from {Source}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            return scrapedProducts;
        }

        private async Task<List<ScrapedProduct>> ScrapePage(StampedHtmlDocument document, ScrapeableLink productLink)
        {
            //var productsNode = document.Document.DocumentNode.SelectSingleNode(ProducstsXpath);
            var productsNode = document.Document.DocumentNode.Descendants().Where(n => n.HasClass("category-product-wrap")).First();
            List<ScrapedProduct> scrapedProducts = new List<ScrapedProduct>();
            List<Task> scrapingTasks = new List<Task>();
            var productsNodes = productsNode.ChildNodes.Where(n => n.HasClass("product-card")).ToList();
            logger.LogInformation($"Scraping products of type {productLink.ProductType}");
            await Task.Run(() =>
            {
                foreach (var productNode in productsNodes)
                {
                    var result = TryGetProductFromNode(productNode, productLink.ProductType);
                    if (result != null)
                    {
                        result.LastChanged = document.Stamp;
                        result.LastChecked = document.Stamp;
                        result.CreationDate = document.Stamp;
                        scrapedProducts.Add(result);
                    }
                    else
                        logger.LogInformation("Failed to scrape product");
                }
            });
            await Task.WhenAll(scrapingTasks);
            return scrapedProducts;
        }

        private async Task<List<ScrapedProduct>> ScrapePage(int page, ScrapeableLink productLink)
        {
            HtmlWeb web = new HtmlWeb();
            StampedHtmlDocument stampedDocument = null;
            string pageUrl = productLink.Link + $"&page={page}";
            await Task.Run(() => stampedDocument = new(web.Load(pageUrl)));
            return await ScrapePage(stampedDocument, productLink);
        }

        private ScrapedProduct TryGetProductFromNode(HtmlNode node, string productType)
        {
            WebClient imageClient = new WebClient();

            try
            {
                var children = node.ChildNodes.Where(n => n.Name == "div").ToArray();
                var header = children[1];
                var brand = node.ChildNodes[3];
                var manufacturer = header.ChildNodes.Where(n => n.HasClass("product-card__brand")).First().ChildNodes.ElementAt(1).GetAttributeValue("alt", null).Replace("\n", string.Empty).Trim();
                var name = brand.ChildNodes[3].ChildNodes[1].InnerText.Replace("\n", string.Empty).Trim();
                var model = brand.ChildNodes[5].ChildNodes[1].InnerText.Replace("\n", string.Empty).Trim();
                var price = TextToPrice(header.ChildNodes.First(n => n.HasClass("product-card__price")).ChildNodes[1].ChildNodes.Last(n => n.Name == "span").InnerText);
                var link = header.ChildNodes.Where(n => n.HasClass("product-card__name")).First().ChildNodes[1].GetAttributeValue("href", null);
                var imageLink = children.Single(n => n.HasClass("product-card__body")).ChildNodes.Single(n => n.HasClass("product-card__image")).ChildNodes.FindFirst("a").Element("img").GetAttributeValue("src", null);
                //var image = await imageClient.DownloadFileAsync(new Uri(imageLink), $"{name}.jpg");
                var result = new ScrapedProduct
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Cat = model,
                    CurrentPrice = price,
                    ProductSource = Source,
                    ProductLink = link,
                    ProductType = productType,
                    Manufacturer = manufacturer,
                    HighestPrice = price,
                    LowestPrice = price,
                    ImageUri = new Uri(imageLink)
                };
                return result;
            }
            catch (Exception ex)
            {
                return null;
                logger.LogInformation($"Failed to get product from node with message: {ex.Message}");
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
}
