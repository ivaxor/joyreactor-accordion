using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.RegularExpressions;

namespace JoyReactor.Accordion.Database;

public partial class SqlDatabaseContext : DbContext
{
    [GeneratedRegex("(?:Data Source|DataSource|Filename)=(.*?)(?:;|$)")]
    private static partial Regex SqliteFilePathRegex();

    public SqlDatabaseContext(DbContextOptions<SqlDatabaseContext> options) : base(options)
    {
        ChangeTracker.LazyLoadingEnabled = false;

        switch (Database.ProviderName)
        {
            case "Microsoft.EntityFrameworkCore.Sqlite":
                var filePath = SqliteFilePathRegex().Match(Database.GetDbConnection().ConnectionString).Groups.Values.Last().Value;
                var directoryPath = Path.GetDirectoryName(filePath);
                Directory.CreateDirectory(directoryPath);
                break;
        }
    }

    public DbSet<PendingImage> PendingImages { get; set; }

    public DbSet<ParsedTag> ParsedTags { get; set; }

    public DbSet<ParsedCoub> ParsedCoubs { get; set; }
    public DbSet<ParsedYoutube> ParsedYoutubes { get; set; }
    public DbSet<ParsedVimeo> ParsedVimeos { get; set; }
    public DbSet<ParsedSoundCloud> ParsedSoundClouds { get; set; }
    public DbSet<ParsedBandCamp> ParsedBandCamps { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SqlDatabaseContext).Assembly);

        switch (Database.ProviderName)
        {
            // SQLite does not have proper support for DateTimeOffset via Entity Framework Core, see the limitations
            // here: https://docs.microsoft.com/en-us/ef/core/providers/sqlite/limitations#query-limitations
            // To work around this, when the Sqlite database provider is used, all model properties of type DateTimeOffset
            // use the DateTimeOffsetToBinaryConverter
            // Based on: https://github.com/aspnet/EntityFrameworkCore/issues/10784#issuecomment-415769754
            // This only supports millisecond precision, but should be sufficient for most use cases.
            case "Microsoft.EntityFrameworkCore.Sqlite":
                foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                    foreach (var property in entityType.ClrType.GetProperties().Where(p => p.PropertyType == typeof(DateTimeOffset) || p.PropertyType == typeof(DateTimeOffset?)))
                        modelBuilder
                            .Entity(entityType.Name)
                            .Property(property.Name)
                            .HasConversion(new DateTimeOffsetToBinaryConverter());
                break;
        }
    }
}