using System.Diagnostics;
using System.Net;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RSS_Reader.Data;
using RSS_Reader.Helpers;
using RSS_Reader.Models;
using RSS_Reader.Models.DataModels;
using RSS_Reader.Services;

namespace RSS_Reader.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FeedParserService _parserService;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context,
        IHttpClientFactory httpClientFactory, FeedParserService parserService)
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
    public async Task<IActionResult> Feed(string id)
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

        // Determine feed type from stored value (or Unknown if null)
        FeedType feedType = DetermineFeedType(feed.FeedType);

        // Fetch the feed content
        HttpClient client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "RSS-Reader/0.1 (+https://github.com/yourrepo)"
        );

        if (feedType != FeedType.Unknown)
            ConfigureAcceptHeaders(client, feedType);

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(url);
        }
        catch (HttpRequestException http)
        {
            if (http.StatusCode != null)
            {
                switch (http.StatusCode)
                {
                    case HttpStatusCode.NotFound:
                        return Json(new { Title = "Feed not found", Message = "The feed you requested could not be found.", Success = false });
                    case HttpStatusCode.Forbidden:
                        return Json(new { Title = "Access denied", Message = "You do not have permission to access this feed.", Success = false });
                    case HttpStatusCode.Unauthorized:
                        return Json(new { Title = "Unauthorized", Message = "You are not authorized to access this feed.", Success = false });
                    default:
                        return Json(new { Title = "Error loading feed", Message = "There was an error loading the feed.", Success = false });
                }
            }
            return Json(new { Title = "Error loading feed", Message = "There was an error loading the feed.", Success = false });
        }
        catch (Exception)
        {
            return Json(new { Title = "Error loading feed", Message = "There was an error loading the feed.", Success = false });
        }

        // Auto-detect feed type from Content-Type if still Unknown
        if (feedType == FeedType.Unknown)
        {
            feedType = DetectFeedType(response.Content.Headers.ContentType?.MediaType);
            feed.FeedType = feedType.ToString();
        }

        // Parse the feed content
        IFeedParser? parser = _parserService.GetParser(feedType);
        if (parser == null)
        {
            _logger.LogWarning("No parser found for feed type {FeedType} (feed: {Id})", feedType, id);
            return Json(new { Title = "Unsupported feed type", Message = "This feed type is not supported.", Success = false });
        }

        ParsedFeed parsed;
        try
        {
            using Stream stream = await response.Content.ReadAsStreamAsync();
            parsed = await parser.ParseAsync(stream);
        }
        catch (JsonFeedFormatException jfEx)
        {
            return Json(new { Title = "Unsupported JSON format", Message = jfEx.Message, Success = false });
        }
        catch (XmlException)
        {
            return Json(new { Title = "Feed not parseable", Message = "The feed URL returned content that could not be parsed as XML.", Success = false });
        }
        catch (FormatException)
        {
            return Json(new { Title = "Feed not parseable", Message = "The feed URL returned content in an unexpected format.", Success = false });
        }

        // Update feed metadata
        if (!string.IsNullOrEmpty(parsed.Title) && string.IsNullOrEmpty(feed.Title))
            feed.Title = parsed.Title;
        feed.Description = parsed.Description ?? feed.Description;

        // Merge parsed entries with existing (dedup by Guid then Link)
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

        return PartialView(feed);
    }

    [HttpPost("/addfeed")]
    public async Task<IActionResult> AddFeed(string title, string url, string? feedType = null)
    {
        try
        {
            _logger.LogInformation("Adding feed {Title} with url {Url} (type: {FeedType})", title, url, feedType ?? "auto");

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
                client.DefaultRequestHeaders.Accept.ParseAdd("application/rss+xml, application/xml");
                break;
            case FeedType.JsonFeed:
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json, application/feed+json");
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
