using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Zapper.Api.Models
{
    public class ImageSettings
    {
        public const string BaseImagePathKey = "ImageSettings";
        public string BasePath { get; set; } = string.Empty;

    }
}
