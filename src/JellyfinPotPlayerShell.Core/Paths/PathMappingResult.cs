namespace JellyfinPotPlayerShell.Core.Paths;

public sealed record PathMappingResult(
    string SourcePath,
    string MappedPath,
    string? RuleId,
    bool IsMapped);
