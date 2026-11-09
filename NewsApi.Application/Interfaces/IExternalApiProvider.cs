using NewsApi.Domain.Entities;

namespace NewsApi.Application.Interfaces
{
    public interface IExternalApiProvider
    {
        string Name { get; }
        Task<IEnumerable<FeedItem>> FetchAsync (string query, CancellationToken cancellationToken);
    }
}
