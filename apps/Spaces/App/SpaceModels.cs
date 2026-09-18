namespace CakeOS.Spaces;

public enum SpaceKind { General, Study, Shopping, Research, Agent }
public enum SpaceThinkingMode { Default, Fast, Balanced, Deep }
public enum SpaceFilePermission { ReadOnly, ReadWrite }
public enum SpaceLayoutPortDirection { Input, Output }

public sealed record SpaceExamplePair(string User, string Assistant);

public sealed record SpaceFileReference(
    string Path,
    string DisplayName,
    SpaceFilePermission Permission,
    DateTimeOffset AddedAt);

public sealed record SpaceGeneratedSurface(string TemplateKey, string InputsJson);

public sealed record SpaceLayoutPort(
    string Id,
    string Label,
    SpaceLayoutPortDirection Direction,
    string DataType = "flow",
    bool AllowsMultipleConnections = true);

public sealed record SpaceLayoutNode(Guid Id, string Category, string Title)
{
    public string Subtitle { get; init; } = string.Empty;
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; } = 220;
    public double Height { get; init; } = 118;
    public IReadOnlyList<SpaceLayoutPort> Ports { get; init; } = [];
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
}

public sealed record SpaceLayoutEdge(Guid Id, Guid FromNodeId, string FromPortId, Guid ToNodeId, string ToPortId)
{
    public string Label { get; init; } = string.Empty;
    public string Branch { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
}

public sealed record SpaceLayoutDocument(IReadOnlyList<SpaceLayoutNode> Nodes, IReadOnlyList<SpaceLayoutEdge> Edges)
{
    public static SpaceLayoutDocument Empty { get; } = new([], []);
}

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
    SpaceLayoutDocument? LayoutDocument = null);

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
