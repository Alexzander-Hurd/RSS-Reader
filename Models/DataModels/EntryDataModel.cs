using System.ComponentModel.DataAnnotations;

namespace RSS_Reader.Models.DataModels;

public class Entry
{
    [Key]
    public string Id { get; set; }
    public string FeedId { get; set; }
    public string? Title { get; set; }
    public string? PubDate { get; set; }
    public string? Link { get; set; }
    public string? Description { get; set; }
}