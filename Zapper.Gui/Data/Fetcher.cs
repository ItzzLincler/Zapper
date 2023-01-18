using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Zapper.Api.Models;

namespace Zapper.Gui.Data
{
    public class FetcherBase
    {
        protected const string ZAPPER_API_ADDRESS = "https://localhost:7216";
    }
    public class ProductFetcher : FetcherBase
    {
        private HttpClient _httpClient = new HttpClient();
        private JsonSerializer serializer = new JsonSerializer();

        public async Task GetProducts()
        {
            return;
        }

        public async Task<List<ScrapedProduct>> GetPage(int page = 1, int itemsPerPage = 100)
        {
            var response = await _httpClient.GetAsync($"{ZAPPER_API_ADDRESS}/api/Product/GetPage?itemsPerPage={itemsPerPage}&page={page}");
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                return new List<ScrapedProduct>();
            var products = await response.Content.ReadJsonAsync<List<ScrapedProduct>>(serializer);
            return products;
        }

        public async Task<List<(int, ScrapedProduct)>> GetIndexedPage(int page = 1, int itemsPerPage = 100)
        {
            var response = await _httpClient.GetAsync($"{ZAPPER_API_ADDRESS}/api/Product/GetPage?itemsPerPage={itemsPerPage}&page={page}");
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                return new List<(int, ScrapedProduct)>();
            var products = await response.Content.ReadJsonAsync<List<ScrapedProduct>>(serializer);
            int index = (page - 1) * itemsPerPage;
            return products.Select((p, i) => (index + i, p)).ToList();
        }

        public string GetImageLink(ScrapedProduct product)
        {
            return $"{ZAPPER_API_ADDRESS}/images/{product.ProductSource}/{product.Id}.jpg";
        }

        public async Task<List<ScrapedProduct>> GetLatestChanged(int count = 20)
        {
            var response = await _httpClient.GetAsync($"{ZAPPER_API_ADDRESS}/api/Change/LatestChanges?count={count}");
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                return null;
            var products = await response.Content.ReadJsonAsync<List<ScrapedProduct>>(serializer);
            return products;
        }

        public async Task<List<(int, ScrapedProduct)>> Search(string term, int page = 1, int itemsPerPage = 100)
        {
            var response = await _httpClient.GetAsync($"{ZAPPER_API_ADDRESS}/api/Product/Search?query={term}");
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                return new List<(int, ScrapedProduct)>();
            var products = await response.Content.ReadJsonAsync<List<ScrapedProduct>>(serializer);
            int index = (page - 1) * itemsPerPage;
            return products.Select((p, i) => (index + i, p)).ToList();
        }

    }
    public class ScraperFetcher : FetcherBase
    {
        private HttpClient _httpClient = new HttpClient();
        private JsonSerializer serializer = new JsonSerializer();
        //public ScraperFetcher()
        //{
        //    serializer.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
        //    serializer.PreserveReferencesHandling= PreserveReferencesHandling.Objects;
        //}

        public async Task<List<(string, TimeSpan)>> GetAllRemainingTime()
        {
            var response = await _httpClient.GetAsync($"{ZAPPER_API_ADDRESS}/api/Scrape/GetAllRemainingTime");
            var result = await response.Content.ReadJsonAsync<List<(string, TimeSpan)>>(serializer);
            return result;
        }

        public async Task<TimeSpan> GetRemainingTime(string sourceName)
        {
            var response = await _httpClient.GetAsync($"{ZAPPER_API_ADDRESS}/api/Scrape/GetRemainingTime");
            var result = await response.Content.ReadJsonAsync<TimeSpan>(serializer);
            return result;
        }

        public async Task<List<string>> GetAvailableScrapers()
        {
            var response = await _httpClient.GetAsync($"{ZAPPER_API_ADDRESS}/api/Scrape/AvailableScrapers");
            var result = await response.Content.ReadJsonAsync<List<string>>(serializer);
            return result;
        }

    }
    public static class HttpContentEx
    {
        public static async Task<T> ReadJsonAsync<T>(this HttpContent httpContent, JsonSerializer serializer)
        {
            var stream = new StreamReader(await httpContent.ReadAsStreamAsync());
            var reader = new JsonTextReader(stream);
            T result;
            try
            {
                result = serializer.Deserialize<T>(reader);

            }
            catch (Exception ex)
            {

                throw ex;
            }
            return result;
        }
    }
}
