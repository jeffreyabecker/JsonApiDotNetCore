using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;
using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;

namespace JsonApiDotNetCoreExample.Models;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
[Resource]
public sealed class RgbColor : Identifiable<int?>
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public override int? Id
    {
        get => base.Id;
        set => base.Id = value;
    }

    [HasOne]
    public Tag Tag { get; set; } = null!;

    protected override string? GetStringId(int? value)
    {
        return value?.ToString("X6");
    }

    protected override int? GetTypedId(string? value)
    {
        return value == null ? null : Convert.ToInt32(value, 16);
    }
}
