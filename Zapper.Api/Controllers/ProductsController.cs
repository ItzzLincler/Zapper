using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Zapper.Api.Data;
using Zapper.Api.Models;

namespace Zapper.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ProductsController : ControllerBase
    {
        private readonly ILogger<ProductsController> _logger;
        private readonly ScrapedProductsContext _scrapedProductsContext;
        public ProductsController(ILogger<ProductsController> logger, ScrapedProductsContext scrapedProductsContext)
        {
            _logger = logger;
            _scrapedProductsContext = scrapedProductsContext;
        }

        [HttpGet(Name = "GetAll")]
        public async Task<ActionResult<IEnumerable<ScrapedProduct>>> GetAll()
        {
            Stopwatch watch = Stopwatch.StartNew();
            var result = await _scrapedProductsContext.ScrapedProducts.ToListAsync();
            var ok = Ok(result);
            watch.Stop();
            _logger.LogInformation($"Retrieved all scarped prodcuts in {watch.Elapsed}");
            return ok;
        }

        [HttpGet(Name = "GetProductById")]
        public async Task<ScrapedProduct> GetProductById(Guid Id)
        {
            return await _scrapedProductsContext.ScrapedProducts.SingleAsync(x => x.Id == Id);
        }

        [HttpGet(Name = "GetProductByCat")]
        public async Task<ScrapedProduct> GetProductByCat(ScrapedProductSource seller, string cat) =>
            await _scrapedProductsContext.ScrapedProducts.SingleAsync(p => p.ProductSource == seller && p.Cat == cat);

        public async Task<ActionResult<IEnumerable<ScrapedProduct>>> GetPagedProducts(int itemsPerPage = 100, int page = 1)
        {
            if (page < 1)
                page = 1;
            int skip = (page - 1) * itemsPerPage;
            var result = await _scrapedProductsContext.ScrapedProducts.Skip(skip).Take(itemsPerPage).ToListAsync();
            return Ok(result);
        }
    }
}