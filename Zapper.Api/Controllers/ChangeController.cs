using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zapper.Api.Data;
using Zapper.Api.Models;

namespace Zapper.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ChangeController : ControllerBase
    {
        private readonly ILogger<ChangeController> _logger;
        private readonly ProductsContext scrapedProductsContext;
        public ChangeController(ILogger<ChangeController> logger, ProductsContext scrapedProductsContext)
        {
            _logger = logger;
            this.scrapedProductsContext = scrapedProductsContext;
        }

        public async Task<ActionResult> LatestChanges(int count = 20)
        {
            var changes = await scrapedProductsContext.ScrapedProducts.Include(p => p.Changes).OrderByDescending(p => p.LastChanged).Where(p => p.Changes.Count > 0).Take(count).ToListAsync();
            return Ok(changes);
        }

        //public async Task<ActionResult> History(Guid productId)
        //{
        //    var result = new ProductHistory
        //    {
        //        Changes = await scrapedProductsContext.ProductPriceChanges.Where(p => p.Id == productId).ToListAsync(),
        //        Product = await scrapedProductsContext.ScrapedProducts.FirstAsync(p => p.Id == productId)
        //    };
        //    return Ok(result);
        //}
    }

}