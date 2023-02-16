using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;
using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;

namespace JsonApiDotNetCoreExample.Models;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
[Resource]
public sealed class Person : Identifiable<long>
{
    [Attr]
    public string? FirstName { get; set; }

    [Attr]
    public string LastName { get; set; } = null!;

    [Attr(Capabilities = AttrCapabilities.AllowView | AttrCapabilities.AllowFilter)]
    [NotMapped]
    public string DisplayName => FirstName != null ? $"{FirstName} {LastName}" : LastName;

    [HasOne]
    public LoginAccount? Account { get; set; }

    [HasMany]
    public ISet<TodoItem> OwnedTodoItems { get; set; } = new HashSet<TodoItem>();

    [HasMany]
    public ISet<TodoItem> AssignedTodoItems { get; set; } = new HashSet<TodoItem>();
}
