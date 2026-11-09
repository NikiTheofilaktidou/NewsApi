using System.ComponentModel.DataAnnotations;

namespace NewsApi.Application.Models
{
    public class FeedRequest
    {
        [Required]
        public string Query { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string SortBy { get; set; } = "date";
        public bool Descending { get; set; } = true;
    }
}
