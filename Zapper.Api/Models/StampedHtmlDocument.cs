using HtmlAgilityPack;

namespace Zapper.Api.Models
{
    public record StampedHtmlDocument
    {
        public HtmlDocument Document { get; init; }
        public DateTime Stamp { get; init; } = DateTime.UtcNow;
        public StampedHtmlDocument(HtmlDocument document)
        {
            Document = document;
        }
    }
}