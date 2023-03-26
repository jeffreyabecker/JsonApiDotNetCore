using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MultiDbContextExample.Models;

namespace MultiDbContextExample.Data;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public sealed class DbContextA : DbContext
{
    public DbSet<ResourceA> ResourceAs => Set<ResourceA>();

    public DbContextA(DbContextOptions<DbContextA> options)
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
