using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Zapper.Api.Data;
using Zapper.Api.Models;

namespace Zapper.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ProductController : ControllerBase
    {
        private readonly ILogger<ProductController> _logger;
        private readonly ProductsContext _scrapedProductsContext;
        public ProductController(ILogger<ProductController> logger, ProductsContext scrapedProductsContext)
        {
            _logger = logger;
            _scrapedProductsContext = scrapedProductsContext;
        }

        [HttpGet(Name = "GetAll")]
        public async Task<ActionResult<IEnumerable<ScrapedProduct>>> GetAll()
        {
            var result = await _scrapedProductsContext.ScrapedProducts.ToListAsync();
            return Ok(result);
        }

        [HttpGet(Name = "GetById")]
        public async Task<ActionResult<ScrapedProduct>> GetById(Guid Id)
        {
            var product = await _scrapedProductsContext.ScrapedProducts.Include(p => p.Changes)
                .SingleAsync(x => x.Id == Id);
            return Ok(product);
        }

        [HttpGet(Name = "GetByCat")]
        public async Task<ActionResult<ScrapedProduct>> GetByCat(ScrapedProductSource seller, string cat)
        {
            var product = await _scrapedProductsContext.ScrapedProducts.Include(p => p.Changes).SingleAsync(p => p.ProductSource == seller && p.Cat == cat);
            return Ok(product);
        }

        public async Task<ActionResult<IEnumerable<ScrapedProduct>>> GetPage(int itemsPerPage = 100, int page = 1)
        {
            if (page < 1)
                page = 1;
            int skip = (page - 1) * itemsPerPage;
            if (skip < 0)
                skip = int.MaxValue;
            var result = await _scrapedProductsContext.ScrapedProducts.OrderBy(p => p.LastChanged).Skip(skip).Take(itemsPerPage).ToListAsync();
            return Ok(result);
        }

        [HttpGet(Name = "GetPageFromSource")]
        public async Task<ActionResult<IEnumerable<ScrapedProduct>>> GetPageFromSource(ScrapedProductSource source, int itemsPerPage = 100, int page = 1)
        {
            if (page < 1)
                return BadRequest($"page cannot be less than 1, requested page: {page}");
            int skip = (page - 1) * itemsPerPage;
            var result = await _scrapedProductsContext.ScrapedProducts.Where(p => p.ProductSource == source).OrderBy(p => p.LastChanged).Skip(skip).Take(itemsPerPage).ToListAsync();
            return Ok(result);
        }

        public async Task<ActionResult> Search(string query)
        {
            var watch = new Stopwatch();
            watch.Start();
            if (query == string.Empty)
                return null;
            if (query.Length > 120)
                return BadRequest("query exceeded limit of 120 ");
            var keyWords = query.ToLower().Split(' ');
            List<ScrapedProduct> result = await _scrapedProductsContext.ScrapedProducts.ToListAsync();
            await Task.Run(() =>
            {
                result = result.Select(p => GetHitCount(p, keyWords))
                    .Where(i => i.Hits > 0).OrderByDescending(p => p.Hits).Select(p => p.Product).ToList();
            });
            Console.WriteLine($"Search for {query} \ntook: {watch.Elapsed}");
            watch.Stop(); 
            return Ok(result);
        }

        private (int Hits, ScrapedProduct Product) GetHitCount(ScrapedProduct product, string[] keyWords)
        {
            int count = 0;
            foreach (var keyword in keyWords)
                if (product.Name.ToLower().Contains(keyword))
                    count++;
            return (count, product);
        }
    }

}