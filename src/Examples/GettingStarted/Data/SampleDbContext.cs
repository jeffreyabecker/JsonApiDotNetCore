using System.Diagnostics;
using GettingStarted.Models;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace GettingStarted.Data;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class SampleDbContext : DbContext
{
    public DbSet<Book> Books => Set<Book>();

    public SampleDbContext(DbContextOptions<SampleDbContext> options)
        : base(options)
    {
    }

#if DEBUG
    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        // Writes SQL statements to the Output Window when debugging.
        builder.LogTo(message => Debug.WriteLine(message), new[]
        {
            DbLoggerCategory.Database.Name
        }, LogLevel.Information);
    }
#endif
}
