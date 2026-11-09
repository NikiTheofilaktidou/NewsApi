using NewsApi.Application.Models;
using NewsApi.Domain.Entities;

namespace NewsApi.Application.Interfaces
{
    public interface IFeedService
    {
        Task<IEnumerable<FeedItem>> GetFeedAsync(FeedRequest request, CancellationToken cancellationToken);
    }
}
