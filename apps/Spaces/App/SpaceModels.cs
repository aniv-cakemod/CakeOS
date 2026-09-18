namespace CakeOS.Spaces;

public enum SpaceKind { General, Study, Shopping, Research, Agent }
public enum SpaceThinkingMode { Default, Fast, Balanced, Deep }
public enum SpaceFilePermission { ReadOnly, ReadWrite }

public sealed record SpaceExamplePair(string User, string Assistant);

public sealed record SpaceFileReference(
    string Path,
    string DisplayName,
    SpaceFilePermission Permission,
    DateTimeOffset AddedAt);

public sealed record SpaceGeneratedSurface(string TemplateKey, string InputsJson);

public sealed record SpaceDefinition(
    Guid Id,
    string Name,
    string Description,
    string IconKey,
    SpaceKind Kind,
    bool IsBuiltIn,
    bool IsArchived,
    string? ModelName,
    string Instructions,
    SpaceThinkingMode ThinkingMode,
    IReadOnlyList<SpaceExamplePair> ExamplePairs,
    IReadOnlyList<SpaceFileReference> Files,
    SpaceGeneratedSurface? GeneratedSurface,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    Guid? ForkedFromSpaceId = null,
    string? LayoutJson = null);

public sealed record SpaceState(
    int Version,
    Guid? CurrentSpaceId,
    IReadOnlyList<SpaceDefinition> Spaces);

public sealed record SpaceConversation(
    Guid Id,
    string Title,
    Guid? SpaceId,
    DateTimeOffset UpdatedAt,
    bool IsArchived = false);
