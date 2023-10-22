using JetBrains.Annotations;

namespace AspNetCore.API.Models;

[PublicAPI]
public sealed class World
{
    public long Id { get; init; }
    public string Theme { get; set; } = String.Empty;
    public string Ecosystem { get; set; } = String.Empty;
    public long Population { get; set; }
    public decimal AvgSurfaceTemp { get; set; }
    public string Name { get; set; } = String.Empty;
}