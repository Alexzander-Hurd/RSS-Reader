namespace RSS_Reader.Services;

public class FeedParserService
{
    private readonly Dictionary<FeedType, IFeedParser> _parsers;

    public FeedParserService(IEnumerable<IFeedParser> parsers)
    {
        _parsers = parsers.ToDictionary(p => p.FeedType);
    }

    public IFeedParser? GetParser(FeedType type) =>
        _parsers.TryGetValue(type, out var parser) ? parser : null;

    public IFeedParser GetDefaultParser() =>
        _parsers.GetValueOrDefault(FeedType.Rss) ?? _parsers.Values.First();
}
