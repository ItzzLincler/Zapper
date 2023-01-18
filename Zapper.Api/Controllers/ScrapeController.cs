using Microsoft.AspNetCore.Mvc;
using Zapper.Api.Models;
using Zapper.Api.Services;

namespace Zapper.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ScrapeController : ControllerBase
    {
        private readonly ILogger<ScrapeController> _logger;
        private readonly object a;
        private readonly PeriodicScrapeService _scrapeService;

        public ScrapeController(ILogger<ScrapeController> logger, PeriodicScrapeService periodicScrapeService)
        {
            _logger = logger;
            _scrapeService = periodicScrapeService;
        }

        public ActionResult<IEnumerable<ScrapedProductSource>> AvailableScrapers()
        {
            return new JsonResult(_scrapeService.GetPeriodicScrapersSources());
        }

        [HttpPost]
        public void AddScrapeToQueue(ScrapedProductSource source)
        {
            _scrapeService.AddScrapingToQueue(source);
        }

        public TimeSpan GetRemainingTime(ScrapedProductSource source)
        {
            return _scrapeService.GetRemainingTime(source);
        }

        public IEnumerable<(ScrapedProductSource, TimeSpan)> GetAllRemainingTime()
        {
            var result = new List<(ScrapedProductSource, TimeSpan)>();
            var sources = _scrapeService.GetPeriodicScrapersSources();
            foreach (var source in sources)
                result.Add((source, GetRemainingTime(source)));
            return result;
        }

        [HttpPost]
        public void SetPeriod(ScrapedProductSource source, TimeSpan period)
        {
            _scrapeService.ChangeScraperPeriod(source, period);
        }
    }
}