using Newtonsoft.Json;

namespace Zapper.Api
{
    public static class HttpContentEx
    {
        public static async Task<T> ReadJsonAsync<T>(this HttpContent httpContent, JsonSerializer serializer)
        {
            var stream = new StreamReader(await httpContent.ReadAsStreamAsync());
            var reader = new JsonTextReader(stream);
            var result = serializer.Deserialize<T>(reader);
            return result;
        }
    }
}
