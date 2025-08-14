using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RSS_Reader.Data;
using RSS_Reader.Models;
using RSS_Reader.Models.DataModels;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Net;
using RSS_Reader.Helpers;

namespace RSS_Reader.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        List<Feed>? feeds = await _context.Feeds.ToListAsync();
        return View(feeds);
    }


    [HttpGet("/feed/{id}")]
    public async Task<IActionResult> Feed(string id)
    {
        Feed? feed = await _context.Feeds.Include(f => f.Entries).FirstOrDefaultAsync(f => f.Id == id);
        if (feed?.Link is not string url) return Json(new
        {
            Title = "Feed not found",
            Message = "The feed you requested could not be found.",
            Success = false
        });

        HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("RSS-Reader/0.1 (+https://github.com/yourrepo)");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/rss+xml, application/xml");
        SyndicationFeed? feedXml = null;
        try
        {
            using Stream stream = await client.GetStreamAsync(url);
            using XmlReader reader = XmlReader.Create(stream);
            feedXml = SyndicationFeed.Load(reader);
        }
        catch (HttpRequestException http)
        {
            if (http.StatusCode != null)
            {
                switch (http.StatusCode)
                {
                    case HttpStatusCode.NotFound:
                        return Json(new
                        {
                            Title = "Feed not found",
                            Message = "The feed you requested could not be found.",
                            Success = false
                        });
                    case HttpStatusCode.Forbidden:
                        return Json(new
                        {
                            Title = "Access denied",
                            Message = "You do not have permission to access this feed.",
                            Success = false
                        });
                    case HttpStatusCode.Unauthorized:
                        return Json(new
                        {
                            Title = "Unauthorized",
                            Message = "You are not authorized to access this feed.",
                            Success = false
                        });
                    default:
                        return Json(new
                        {
                            Title = "Error loading feed",
                            Message = "There was an error loading the feed.",
                            Success = false
                        });
                }
            }
            else
            {
                return Json(new
                {
                    Title = "Error loading feed",
                    Message = "There was an error loading the feed.",
                    Success = false
                });
            }
        }
        catch (Exception)
        {
            return Json(new
            {
                Title = "Error loading feed",
                Message = "There was an error loading the feed.",
                Success = false
            });
        }


        if (feedXml == null) return Json(new
        {
            Title = "Feed not found",
            Message = "The feed you requested could not be found.",
            Success = false
        });

        feed.Description = feedXml.Description.Text ?? "";


        List<Entry> entries = feed.Entries ?? new List<Entry>();
        foreach (SyndicationItem item in feedXml.Items)
        {
            string? link = item.Links.FirstOrDefault()?.Uri.ToString();
            string? guid = item.Id;
            Entry? entry = null;

            if (string.IsNullOrEmpty(guid))
            {
                if (string.IsNullOrEmpty(link)) continue;
                entry = entries.FirstOrDefault(e => e.Link == link);
            }
            else
            {
                entry = entries.FirstOrDefault(e => e.Guid == guid);
            }

            if (entry == null)
            {
                string? contentEncoded = item.ElementExtensions
                    .FirstOrDefault(e => e.OuterName == "encoded")?
                    .GetObject<string>();

                string? atomContent = item.Content is TextSyndicationContent content
                    ? content.Text
                    : null;

                string? summary = item.Summary is TextSyndicationContent summaryContent
                    ? summaryContent.Text
                    : null;

                string? FullContent = "";

                if (!string.IsNullOrEmpty(contentEncoded)) FullContent = contentEncoded;
                else if (!string.IsNullOrEmpty(atomContent) && atomContent != summary && atomContent.Length > summary?.Length) FullContent = atomContent;
                if (!string.IsNullOrEmpty(summary))
                {
                    if (string.IsNullOrEmpty(FullContent) && string.IsNullOrEmpty(atomContent) && summary.Length > 1000) FullContent = summary;

                    summary = HtmlUtils.RemoveHtmlTags(summary);
                    if (summary.Length > 300) summary = summary.Substring(0, 300) + "..."; // Truncate long summaries
                }



                entry = new Entry()
                {
                    Id = Guid.NewGuid().ToString(),
                    FeedId = id,
                    Title = item.Title.Text,
                    PubDate = item.PublishDate.UtcDateTime == default ? DateTime.UtcNow : item.PublishDate.UtcDateTime,
                    Link = link ?? "",
                    Description = summary ?? "",
                    FullContent = FullContent,
                    Guid = guid
                };
                _context.Entries.Add(entry);
                entries.Add(entry);
            }
        }

        feed.Entries = entries.OrderByDescending(e => e.PubDate).ToList();
        _context.Feeds.Update(feed);
        await _context.SaveChangesAsync();
        return PartialView(feed);
    }

    [HttpPost("/addfeed")]
    public async Task<IActionResult> AddFeed(string title, string url)
    {
        try
        {
            _logger.LogInformation($"Adding feed {title} with url {url}");

            Feed? feed = await _context.Feeds.FirstOrDefaultAsync(f => f.Link == url);
            if (feed == null)
            {
                feed = new Feed()
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = title,
                    Link = url,
                    Entries = new List<Entry>()
                };
                _context.Feeds.Add(feed);
                await _context.SaveChangesAsync();

                return Json(feed);
            }
            else
            {
                return Json(new { Title = "Feed already exists", Message = "The feed already exists in the database", Success = false });
            }
        }
        catch (System.Exception)
        {
            return Json(new { Title = "Error adding feed", Message = "Failed to add feed to the database", Success = false });
        }
    }

    [HttpPost]
    [Route("/deletefeed/{id}")]
    public async Task<IActionResult> DeleteFeed(string id)
    {
        Feed? feed = await _context.Feeds.FirstOrDefaultAsync(f => f.Id == id);
        if (feed == null) return Json(new { Title = "Feed not found", Message = "The feed you requested could not be found.", Success = false });

        Entry[] entries = await _context.Entries.Where(e => e.FeedId == id).ToArrayAsync();
        _context.Entries.RemoveRange(entries);

        _context.Feeds.Remove(feed);
        await _context.SaveChangesAsync();
        return Json(new { Title = "Feed deleted", Message = "The feed has been deleted.", Success = true });
    }

    [HttpGet]
    [Route("/article/{id}")]
    public async Task<IActionResult> Article(string id)
    {
        Entry? entry = await _context.Entries.FirstOrDefaultAsync(e => e.Id == id);
        if (entry == null) return Json(new { Title = "Article not found", Message = "The article you requested could not be found.", Success = false });
        ViewBag.standalone = false;

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return PartialView(entry);
        ViewBag.standalone = true;
        return View(entry);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new
        {
            Title = "Error loading page",
            Message = "There was an error loading the page, please try again",
            Success = false
        });
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
