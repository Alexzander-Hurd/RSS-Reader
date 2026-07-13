namespace RSS_Reader.Services;

public class ParsedFeed
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<ParsedEntry> Entries { get; set; } = new();
}

public class ParsedEntry
{
    public string? Title { get; set; }
    public string? Link { get; set; }
    public string? Guid { get; set; }
    public string? Description { get; set; }
    public string? FullContent { get; set; }
    public DateTime PubDate { get; set; }
}
