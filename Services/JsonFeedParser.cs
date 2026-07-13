using System.Text.Json;

namespace RSS_Reader.Services;

public class JsonFeedParser : IFeedParser
{
    public FeedType FeedType => FeedType.JsonFeed;

    public Task<ParsedFeed> ParseAsync(Stream content, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        // Validate it's a JSON Feed v1.x document
        if (!root.TryGetProperty("version", out var versionEl)
            || versionEl.ValueKind != JsonValueKind.String)
        {
            throw new JsonFeedFormatException(
                "This feed is not in JSON Feed v1.1 format. Custom JSON schema mapping will be supported in a future update."
            );
        }

        string? version = versionEl.GetString();
        if (string.IsNullOrEmpty(version) || !version.Contains("jsonfeed.org"))
        {
            throw new JsonFeedFormatException(
                "This feed is not in JSON Feed v1.1 format. Custom JSON schema mapping will be supported in a future update."
            );
        }

        var result = new ParsedFeed
        {
            Title = root.TryGetProperty("title", out var titleEl) && titleEl.ValueKind == JsonValueKind.String
                ? titleEl.GetString() : null,
            Description = root.TryGetProperty("description", out var descEl) && descEl.ValueKind == JsonValueKind.String
                ? descEl.GetString() : null
        };

        // Items array is optional per spec (may be empty)
        if (root.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var itemEl in itemsEl.EnumerateArray())
            {
                var entry = ParseItem(itemEl);
                if (entry != null)
                    result.Entries.Add(entry);
            }
        }

        return Task.FromResult(result);
    }

    private static ParsedEntry? ParseItem(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object)
            return null;

        // ID is required per spec; fallback to url, then generate one
        string? guid = null;
        if (item.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String)
            guid = idEl.GetString();

        if (string.IsNullOrEmpty(guid))
        {
            if (item.TryGetProperty("url", out var urlEl) && urlEl.ValueKind == JsonValueKind.String)
                guid = urlEl.GetString();
            else
                guid = Guid.NewGuid().ToString();
        }

        // Link: preferred url, fallback external_url
        string? link = null;
        if (item.TryGetProperty("url", out var linkEl) && linkEl.ValueKind == JsonValueKind.String)
            link = linkEl.GetString();
        else if (item.TryGetProperty("external_url", out var extEl) && extEl.ValueKind == JsonValueKind.String)
            link = extEl.GetString();

        // Title
        string? title = item.TryGetProperty("title", out var titleEl) && titleEl.ValueKind == JsonValueKind.String
            ? titleEl.GetString() : null;

        // Full content: prefer content_html over content_text
        string? fullContent = null;
        if (item.TryGetProperty("content_html", out var htmlEl) && htmlEl.ValueKind == JsonValueKind.String)
            fullContent = htmlEl.GetString();
        else if (item.TryGetProperty("content_text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
            fullContent = textEl.GetString();

        // Summary (raw — controller strips HTML if needed)
        string? summary = item.TryGetProperty("summary", out var sumEl) && sumEl.ValueKind == JsonValueKind.String
            ? sumEl.GetString() : null;

        // Date: prefer date_modified over date_published
        DateTime pubDate = DateTime.UtcNow;
        string? dateModified = item.TryGetProperty("date_modified", out var modEl) && modEl.ValueKind == JsonValueKind.String
            ? modEl.GetString() : null;
        string? datePublished = item.TryGetProperty("date_published", out var pubEl) && pubEl.ValueKind == JsonValueKind.String
            ? pubEl.GetString() : null;

        string? dateStr = dateModified ?? datePublished;
        if (dateStr != null && DateTime.TryParse(dateStr, out var parsedDate))
            pubDate = parsedDate;

        return new ParsedEntry
        {
            Title = title ?? "Untitled",
            Link = link ?? "",
            Guid = guid,
            Description = summary ?? "",
            FullContent = fullContent ?? "",
            PubDate = pubDate
        };
    }
}
