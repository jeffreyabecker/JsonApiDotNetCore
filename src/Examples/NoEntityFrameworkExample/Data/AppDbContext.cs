using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using NoEntityFrameworkExample.Models;

namespace NoEntityFrameworkExample.Data;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public sealed class AppDbContext : DbContext
{
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
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
