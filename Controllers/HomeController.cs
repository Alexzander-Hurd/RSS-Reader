using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RSS_Reader.Data;
using RSS_Reader.Models;
using RSS_Reader.Models.DataModels;
using System.ServiceModel.Syndication;
using System.Xml;

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
        using var stream = await client.GetStreamAsync(url);
        using var reader = XmlReader.Create(stream);
        SyndicationFeed? feedXml = SyndicationFeed.Load(reader);

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
            Entry? entry = null;
            if (string.IsNullOrEmpty(link))
            {
                string? guid = item.Id;
                if (string.IsNullOrEmpty(guid)) continue;
                entry = entries.FirstOrDefault(e => e.Guid == guid);
            }
            else
            {
                entry = entries.FirstOrDefault(e => e.Link == link);
            }

            if (entry == null)
            {
                entry = new Entry()
                {
                    Id = Guid.NewGuid().ToString(),
                    FeedId = id,
                    Title = item.Title.Text,
                    PubDate = item.PublishDate.UtcDateTime,
                    Link = item.Links[0].Uri.ToString(),
                    Description = item.Summary?.Text
                };
                _context.Entries.Add(entry);
                entries.Add(entry);
            }
        }

        feed.Entries = entries;
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
