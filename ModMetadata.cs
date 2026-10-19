using Range = SemanticVersioning.Range;
using SPTarkov.Server.Core.Models.Spt.Mod;
using Version = SemanticVersioning.Version;

namespace PortableTraders;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.portabletraders.core";
    public string Name { get; init; } = "PortableTraders";
    public string Author { get; init; } = "PortableTraders";
    public List<string>? Contributors { get; init; } = null;
    public Version Version { get; init; } = new("1.1.0", false);
    public Range SptVersion { get; init; } = new("~4.1.2", false);
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; } = null;
    public Dictionary<string, Range>? ModDependencies { get; init; } = null;
    public string? Url { get; init; } = null;
    public string License { get; init; } = "MIT";
}
