using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace JsonApiDotNetCoreTests.IntegrationTests.CustomQueryFilters.IsUpperCase;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public sealed class IsUpperCaseDbContext : DbContext
{
    public DbSet<Newspaper> Newspapers => Set<Newspaper>();
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();

    public IsUpperCaseDbContext(DbContextOptions<IsUpperCaseDbContext> options)
        : base(options)
    {
    }
}
