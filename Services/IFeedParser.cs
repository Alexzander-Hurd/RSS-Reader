namespace RSS_Reader.Services;

public interface IFeedParser
{
    FeedType FeedType { get; }
    Task<ParsedFeed> ParseAsync(Stream content, CancellationToken ct = default);
}
