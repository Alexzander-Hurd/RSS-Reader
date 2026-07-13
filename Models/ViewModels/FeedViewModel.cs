using RSS_Reader.Models.DataModels;

namespace RSS_Reader.Models.ViewModels;

public class FeedViewModel
{
    public Feed Feed { get; set; } = default!;
    public List<Entry> Entries { get; set; } = new();

    // Pagination
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
    public int PerPage { get; set; } = 20;
    public int ShowingFrom { get; set; }
    public int ShowingTo { get; set; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    // Active filter controls state
    public string? Query { get; set; }
    public string SortBy { get; set; } = "newest";
    public string FilterBy { get; set; } = "all";

    // Error state (displayed as a banner in the view)
    public string? FeedError { get; set; }

    public void ComputeDisplayRange()
    {
        ShowingFrom = TotalCount == 0 ? 0 : (CurrentPage - 1) * PerPage + 1;
        ShowingTo = Math.Min(CurrentPage * PerPage, TotalCount);
    }
}
