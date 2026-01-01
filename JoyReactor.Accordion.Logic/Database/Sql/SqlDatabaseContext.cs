using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace JoyReactor.Accordion.Logic.Database.Sql;

public partial class SqlDatabaseContext : DbContext
{
    public SqlDatabaseContext(DbContextOptions<SqlDatabaseContext> options) : base(options)
    {
        ChangeTracker.LazyLoadingEnabled = false;
    }

    public DbSet<ParsedBandCamp> ParsedBandCamps { get; set; }
    public DbSet<ParsedCoub> ParsedCoubs { get; set; }
    public DbSet<ParsedPost> ParsedPost { get; set; }
    public DbSet<ParsedPostAttributeEmbeded> ParsedPostAttributeEmbeds { get; set; }
    public DbSet<ParsedPostAttributePicture> ParsedPostAttributePictures { get; set; }
    public DbSet<ParsedSoundCloud> ParsedSoundClouds { get; set; }
    public DbSet<ParsedTag> ParsedTags { get; set; }
    public DbSet<ParsedVimeo> ParsedVimeos { get; set; }
    public DbSet<ParsedYoutube> ParsedYoutubes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SqlDatabaseContext).Assembly);
    }
}