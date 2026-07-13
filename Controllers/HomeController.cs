using System.Diagnostics;
using System.Net;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RSS_Reader.Data;
using RSS_Reader.Helpers;
using RSS_Reader.Models;
using RSS_Reader.Models.DataModels;
using RSS_Reader.Models.ViewModels;
using RSS_Reader.Services;

namespace RSS_Reader.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FeedParserService _parserService;

    public HomeController(
        ILogger<HomeController> logger,
        ApplicationDbContext context,
        IHttpClientFactory httpClientFactory,
        FeedParserService parserService
    )
    {
        _logger = logger;
        _context = context;
        _httpClientFactory = httpClientFactory;
        _parserService = parserService;
    }

    public async Task<IActionResult> Index()
    {
        List<Feed>? feeds = await _context.Feeds.ToListAsync();

        Dictionary<string, int> unreadCounts = await _context
            .Entries.Where(e => !e.IsRead)
            .GroupBy(e => e.FeedId)
            .Select(g => new { FeedId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.FeedId, x => x.Count);
        ViewBag.UnreadCounts = unreadCounts;

        return View(feeds);
    }

    [HttpGet("/feed/{id}")]
    public async Task<IActionResult> Feed(
        string id,
        string? q = null,
        string? sort_by = null,
        string? filter_by = null,
        int page = 1,
        int per_page = 20
    )
    {
        Feed? feed = await _context
            .Feeds.Include(f => f.Entries)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (feed?.Link is not string url)
            return Json(
                new
                {
                    Title = "Feed not found",
                    Message = "The feed you requested could not be found.",
                    Success = false,
                }
            );

        // Determine if this is a filter/search-only request (skip remote fetch)
        bool isFilterRequest = !string.IsNullOrEmpty(q)
            || !string.IsNullOrEmpty(filter_by)
            || !string.IsNullOrEmpty(sort_by) && sort_by != "newest"
            || page > 1
            || per_page != 20;

        string? feedError = null;

        // ── Remote fetch (only when no filter params) ──
        if (!isFilterRequest)
        {
            FeedType feedType = DetermineFeedType(feed.FeedType);
            HttpClient client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "RSS-Reader/0.1 (+https://github.com/yourrepo)"
            );

            if (feedType != FeedType.Unknown)
                ConfigureAcceptHeaders(client, feedType);

            bool fetchSucceeded = false;
            HttpResponseMessage? response = null;
            IFeedParser? parser = null;
            ParsedFeed? parsed = null;

            try
            {
                response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                // Auto-detect feed type from Content-Type if still Unknown
                if (feedType == FeedType.Unknown)
                {
                    feedType = DetectFeedType(response.Content.Headers.ContentType?.MediaType);
                    feed.FeedType = feedType.ToString();
                }

                parser = _parserService.GetParser(feedType);
                if (parser == null)
                {
                    _logger.LogWarning("No parser found for feed type {FeedType} (feed: {Id})", feedType, id);
                    feedError = "This feed type is not currently supported.";
                }
                else
                {
                    using Stream stream = await response.Content.ReadAsStreamAsync();
                    parsed = await parser.ParseAsync(stream);
                    fetchSucceeded = true;
                }
            }
            catch (HttpRequestException http)
            {
                feedError = http.StatusCode switch
                {
                    HttpStatusCode.NotFound => "The feed URL returned a 404 Not Found error.",
                    HttpStatusCode.Forbidden => "Access denied (403 Forbidden).",
                    HttpStatusCode.Unauthorized => "Authentication required (401 Unauthorized).",
                    _ => "There was an HTTP error loading the feed.",
                };
            }
            catch (JsonFeedFormatException jfEx)
            {
                feedError = jfEx.Message;
            }
            catch (XmlException)
            {
                feedError = "The feed URL returned content that could not be parsed.";
            }
            catch (FormatException)
            {
                feedError = "The feed URL returned content in an unexpected format.";
            }
            catch (Exception)
            {
                feedError = "Network error: unable to connect to the feed URL.";
            }

            // Merge and save on success
            if (fetchSucceeded && parsed != null)
            {
                if (!string.IsNullOrEmpty(parsed.Title) && string.IsNullOrEmpty(feed.Title))
                    feed.Title = parsed.Title;
                feed.Description = parsed.Description ?? feed.Description;

                List<Entry> entries = feed.Entries ?? new List<Entry>();
                foreach (var parsedEntry in parsed.Entries)
                {
                    if (string.IsNullOrEmpty(parsedEntry.Guid) && string.IsNullOrEmpty(parsedEntry.Link))
                        continue;

                    Entry? existing = FindExistingEntry(entries, parsedEntry);
                    if (existing == null)
                    {
                        entries.Add(CreateEntryFromParsed(feed.Id, parsedEntry));
                    }
                }

                feed.Entries = entries.OrderByDescending(e => e.PubDate).ToList();
                _context.Feeds.Update(feed);
                await _context.SaveChangesAsync();
            }
        }

        // ── Build viewmodel with search/filter/sort/pagination ──
        var entriesQuery = (feed.Entries ?? new List<Entry>()).AsEnumerable();

        // Omni-search: split query by spaces, each term must match title OR description
        if (!string.IsNullOrEmpty(q))
        {
            var terms = q.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );
            foreach (var term in terms)
            {
                // Local variable capture for term
                string t = term;
                entriesQuery = entriesQuery.Where(e =>
                    (e.Title != null && e.Title.Contains(t, StringComparison.OrdinalIgnoreCase))
                    || (
                        e.Description != null
                        && e.Description.Contains(t, StringComparison.OrdinalIgnoreCase)
                    )
                );
            }
        }

        // Filter
        if (filter_by == "unread")
            entriesQuery = entriesQuery.Where(e => !e.IsRead);

        // Sort
        sort_by ??= "newest";
        entriesQuery =
            sort_by == "oldest"
                ? entriesQuery.OrderBy(e => e.PubDate)
                : entriesQuery.OrderByDescending(e => e.PubDate);

        // Paginate
        int totalCount = entriesQuery.Count();
        int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)per_page));
        page = Math.Clamp(page, 1, totalPages);
        var pagedEntries = entriesQuery
            .Skip((page - 1) * per_page)
            .Take(per_page)
            .ToList();

        var vm = new FeedViewModel
        {
            Feed = feed,
            Entries = pagedEntries,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PerPage = per_page,
            Query = q,
            SortBy = sort_by,
            FilterBy = filter_by ?? "all",
            FeedError = feedError,
        };
        vm.ComputeDisplayRange();

        // Return only the entries fragment when requested by AJAX filter controls
        if (Request.Headers["X-Feed-Partial"] == "entries")
            return PartialView("_FeedEntries", vm);

        return PartialView(vm);
    }

    [HttpPost("/addfeed")]
    public async Task<IActionResult> AddFeed(string title, string url, string? feedType = null)
    {
        try
        {
            _logger.LogInformation(
                "Adding feed {Title} with url {Url} (type: {FeedType})",
                title,
                url,
                feedType ?? "auto"
            );

            Feed? feed = await _context.Feeds.FirstOrDefaultAsync(f => f.Link == url);
            if (feed == null)
            {
                feed = new Feed()
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = title,
                    Link = url,
                    FeedType = string.IsNullOrWhiteSpace(feedType) ? null : feedType,
                    Entries = new List<Entry>(),
                };
                _context.Feeds.Add(feed);
                await _context.SaveChangesAsync();

                return Json(feed);
            }
            else
            {
                return Json(
                    new
                    {
                        Title = "Feed already exists",
                        Message = "The feed already exists in the database",
                        Success = false,
                    }
                );
            }
        }
        catch (System.Exception)
        {
            return Json(
                new
                {
                    Title = "Error adding feed",
                    Message = "Failed to add feed to the database",
                    Success = false,
                }
            );
        }
    }

    [HttpPost]
    [Route("/deletefeed/{id}")]
    public async Task<IActionResult> DeleteFeed(string id)
    {
        Feed? feed = await _context.Feeds.FirstOrDefaultAsync(f => f.Id == id);
        if (feed == null)
            return Json(
                new
                {
                    Title = "Feed not found",
                    Message = "The feed you requested could not be found.",
                    Success = false,
                }
            );

        Entry[] entries = await _context.Entries.Where(e => e.FeedId == id).ToArrayAsync();
        _context.Entries.RemoveRange(entries);

        _context.Feeds.Remove(feed);
        await _context.SaveChangesAsync();
        return Json(
            new
            {
                Title = "Feed deleted",
                Message = "The feed has been deleted.",
                Success = true,
            }
        );
    }

    [HttpGet]
    [Route("/article/{id}")]
    public async Task<IActionResult> Article(string id)
    {
        Entry? entry = await _context.Entries.FirstOrDefaultAsync(e => e.Id == id);
        if (entry == null)
            return Json(
                new
                {
                    Title = "Article not found",
                    Message = "The article you requested could not be found.",
                    Success = false,
                }
            );

        // Auto-mark as read when viewed
        if (!entry.IsRead)
        {
            entry.IsRead = true;
            await _context.SaveChangesAsync();
        }

        ViewBag.standalone = false;

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView(entry);
        ViewBag.standalone = true;
        return View(entry);
    }

    [HttpPost]
    [Route("/article/{id}/read")]
    public async Task<IActionResult> ToggleRead(string id)
    {
        Entry? entry = await _context.Entries.FirstOrDefaultAsync(e => e.Id == id);
        if (entry == null)
            return Json(new { success = false, message = "Article not found." });

        entry.IsRead = !entry.IsRead;
        await _context.SaveChangesAsync();

        return Json(new { success = true, isRead = entry.IsRead });
    }

    [HttpPost]
    [Route("/feed/{id}/read")]
    public async Task<IActionResult> MarkAllRead(string id)
    {
        Feed? feed = await _context.Feeds.FirstOrDefaultAsync(f => f.Id == id);
        if (feed == null)
            return Json(new { success = false, message = "Feed not found." });

        int count = await _context
            .Entries.Where(e => e.FeedId == id && !e.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.IsRead, true));

        return Json(new { success = true, count });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(
                new
                {
                    Title = "Error loading page",
                    Message = "There was an error loading the page, please try again",
                    Success = false,
                }
            );
        return View(
            new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }
        );
    }

    // --- Private helpers ---

    private static FeedType DetermineFeedType(string? storedType)
    {
        if (string.IsNullOrEmpty(storedType))
            return FeedType.Unknown;

        if (Enum.TryParse<FeedType>(storedType, ignoreCase: true, out var parsed))
            return parsed;

        return FeedType.Unknown;
    }

    private static FeedType DetectFeedType(string? mediaType)
    {
        if (string.IsNullOrEmpty(mediaType))
            return FeedType.Unknown;

        // Common RSS/XML media types
        if (mediaType.Contains("xml") || mediaType.Contains("rss") || mediaType.Contains("atom"))
            return FeedType.Rss;

        // JSON media types
        if (mediaType.Contains("json") || mediaType.Contains("feed+json"))
            return FeedType.JsonFeed;

        return FeedType.Unknown;
    }

    private static void ConfigureAcceptHeaders(HttpClient client, FeedType feedType)
    {
        switch (feedType)
        {
            case FeedType.Rss:
                client.DefaultRequestHeaders.Accept.ParseAdd(
                    "application/rss+xml, application/xml"
                );
                break;
            case FeedType.JsonFeed:
                client.DefaultRequestHeaders.Accept.ParseAdd(
                    "application/json, application/feed+json"
                );
                break;
        }
    }

    private static Entry? FindExistingEntry(List<Entry> entries, ParsedEntry parsed)
    {
        // Try matching by Guid first
        if (!string.IsNullOrEmpty(parsed.Guid))
        {
            var match = entries.FirstOrDefault(e => e.Guid == parsed.Guid);
            if (match != null)
                return match;
        }

        // Fallback to Link
        if (!string.IsNullOrEmpty(parsed.Link))
        {
            var match = entries.FirstOrDefault(e => e.Link == parsed.Link);
            if (match != null)
                return match;
        }

        return null;
    }

    private static Entry CreateEntryFromParsed(string feedId, ParsedEntry parsed)
    {
        string? description = null;
        if (!string.IsNullOrEmpty(parsed.Description))
        {
            description = HtmlUtils.RemoveHtmlTags(parsed.Description);
            if (description.Length > 300)
                description = description[..300] + "...";
        }

        return new Entry
        {
            Id = Guid.NewGuid().ToString(),
            FeedId = feedId,
            Title = parsed.Title ?? "Untitled",
            PubDate = parsed.PubDate,
            Link = parsed.Link ?? "",
            Description = description ?? "",
            FullContent = parsed.FullContent ?? "",
            Guid = parsed.Guid,
        };
    }
}
