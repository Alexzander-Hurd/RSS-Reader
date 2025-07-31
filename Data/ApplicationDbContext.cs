using RSS_Reader.Models.DataModels;
using Microsoft.EntityFrameworkCore;

namespace RSS_Reader.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Feed> Feeds { get; set; }
    public DbSet<Entry> Entries { get; set; }
}