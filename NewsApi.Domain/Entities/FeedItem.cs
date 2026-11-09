
namespace NewsApi.Domain.Entities
{
    public class FeedItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Provider { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Url { get; set; }
        public DateTime PublishedAt { get; set; }
        public string Category { get; set; } = string.Empty;
    }
}
