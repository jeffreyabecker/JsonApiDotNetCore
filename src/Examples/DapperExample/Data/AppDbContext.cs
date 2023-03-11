using System.Diagnostics;
using DapperExample.Models;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

// @formatter:wrap_chained_method_calls chop_always

namespace DapperExample.Data;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public sealed class AppDbContext : DbContext
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<LoginAccount> LoginAccounts => Set<LoginAccount>();
    public DbSet<AccountRecovery> AccountRecoveries => Set<AccountRecovery>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<RgbColor> RgbColors => Set<RgbColor>();

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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // When deleting a person, un-assign him/her from existing todo-items.
        builder.Entity<Person>()
            .HasMany(person => person.AssignedTodoItems)
            .WithOne(todoItem => todoItem.Assignee!);

        // When deleting a person, the todo-items he/she owns are deleted too.
        builder.Entity<Person>()
            .HasMany(person => person.OwnedTodoItems)
            .WithOne(todoItem => todoItem.Owner);

        builder.Entity<Person>()
            .HasOne(person => person.Account)
            .WithOne(loginAccount => loginAccount.Person)
            .HasForeignKey<Person>("AccountId");

        builder.Entity<LoginAccount>()
            .HasOne(loginAccount => loginAccount.Recovery)
            .WithOne(accountRecovery => accountRecovery.Account)
            .HasForeignKey<LoginAccount>("RecoveryId");

        builder.Entity<TodoItem>()
            .Property(todoItem => todoItem.Priority);

        builder.Entity<Tag>()
            .HasOne(tag => tag.Color)
            .WithOne(rgbColor => rgbColor.Tag)
            .HasForeignKey<RgbColor>("TagId");

        AdjustDeleteBehaviorForJsonApi(builder);
    }

    private static void AdjustDeleteBehaviorForJsonApi(ModelBuilder builder)
    {
        foreach (IMutableForeignKey foreignKey in builder.Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetForeignKeys()))
        {
            if (foreignKey.DeleteBehavior == DeleteBehavior.ClientSetNull)
            {
                foreignKey.DeleteBehavior = DeleteBehavior.SetNull;
            }

            if (foreignKey.DeleteBehavior == DeleteBehavior.ClientCascade)
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Cascade;
            }
        }
    }
}