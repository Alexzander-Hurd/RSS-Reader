using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RSS_Reader.Data;
using RSS_Reader.Models;
using RSS_Reader.Models.DataModels;

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
        if (feed == null) return Json(new {
            Title ="Feed not found",
            Message ="The feed you requested could not be found.",
            Success =false});
        return PartialView(feed);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
