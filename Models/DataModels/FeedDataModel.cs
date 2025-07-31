using System.ComponentModel.DataAnnotations;

namespace RSS_Reader.Models.DataModels;

public class Feed
{
    [Key]
    public string Id { get; set; }
    public string? Title { get; set; }
    public string? Link { get; set; }
    public string? Description { get; set; }
    public List<Entry>? Entries { get; set; }

}