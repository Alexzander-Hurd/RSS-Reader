using System.ServiceModel.Syndication;
using System.Xml;

namespace RSS_Reader.Services;

public class RssFeedParser : IFeedParser
{
    public FeedType FeedType => FeedType.Rss;

    public Task<ParsedFeed> ParseAsync(Stream content, CancellationToken ct = default)
    {
        using var reader = XmlReader.Create(content);
        var feedXml = SyndicationFeed.Load(reader);

        var result = new ParsedFeed
        {
            Title = feedXml.Title?.Text,
            Description = feedXml.Description?.Text,
        };

        foreach (SyndicationItem item in feedXml.Items)
        {
            string? link = item.Links.FirstOrDefault()?.Uri?.ToString();
            string? guid = item.Id;

            // Extract content:encoded (common in RSS feeds like WordPress)
            string? contentEncoded = item
                .ElementExtensions.FirstOrDefault(e => e.OuterName == "encoded")
                ?.GetObject<string>();

            // Extract Atom content element
            string? atomContent = item.Content is TextSyndicationContent atomContentEl
                ? atomContentEl.Text
                : null;

            // Extract summary
            string? summary = item.Summary is TextSyndicationContent summaryContent
                ? summaryContent.Text
                : null;

            // Determine full content: prefer content:encoded > atom content > long summary
            string? fullContent = null;

            if (!string.IsNullOrEmpty(contentEncoded))
                fullContent = contentEncoded;
            else if (
                !string.IsNullOrEmpty(atomContent)
                && atomContent != summary
                && atomContent.Length > summary?.Length
            )
                fullContent = atomContent;

            if (!string.IsNullOrEmpty(summary))
            {
                // Use summary as full content only if nothing else is available and it's long
                if (
                    string.IsNullOrEmpty(fullContent)
                    && string.IsNullOrEmpty(atomContent)
                    && summary.Length > 1000
                )
                    fullContent = summary;
            }

            // Date handling: prefer LastUpdatedTime over PublishDate
            DateTime pubDate =
                item.PublishDate.UtcDateTime == default
                    ? DateTime.UtcNow
                    : item.PublishDate.UtcDateTime;

            DateTime updatedDate =
                item.LastUpdatedTime.UtcDateTime == default
                    ? pubDate
                    : item.LastUpdatedTime.UtcDateTime;

            result.Entries.Add(
                new ParsedEntry
                {
                    Title = item.Title?.Text ?? "Untitled",
                    Link = link ?? "",
                    Guid = guid,
                    Description = summary ?? "",
                    FullContent = fullContent ?? "",
                    PubDate = updatedDate,
                }
            );
        }

        return Task.FromResult(result);
    }
}
