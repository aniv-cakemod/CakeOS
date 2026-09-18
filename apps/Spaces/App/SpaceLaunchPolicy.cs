namespace CakeOS.Spaces;

public enum SpaceLaunchTarget { Chat, Study, Tasks }

public sealed record SpaceLaunchPlan(
    SpaceLaunchTarget Target,
    string Title,
    string? ModelName,
    SpaceThinkingMode ThinkingMode,
    string RegisteredContext,
    IReadOnlyList<SpaceFileReference> Files);

public static class SpaceLaunchPolicy
{
    public static SpaceLaunchPlan Resolve(SpaceDefinition space)
    {
        ArgumentNullException.ThrowIfNull(space);
        var target = space.Kind switch
        {
            SpaceKind.Study => SpaceLaunchTarget.Study,
            SpaceKind.Agent => SpaceLaunchTarget.Tasks,
            _ => SpaceLaunchTarget.Chat
        };
        return new SpaceLaunchPlan(target, space.Name, space.ModelName, space.ThinkingMode, BuildContext(space), space.Files.ToArray());
    }

    public static string BuildContext(SpaceDefinition space)
    {
        var parts = new List<string> { $"Active Cake Space: {space.Name}.", $"Space kind: {space.Kind}." };
        if (!string.IsNullOrWhiteSpace(space.Description)) parts.Add("Purpose: " + space.Description.Trim());
        if (!string.IsNullOrWhiteSpace(space.Instructions)) parts.Add("Space instructions:\n" + space.Instructions.Trim());
        if (space.ExamplePairs.Count > 0)
            parts.Add("Space examples:\n" + string.Join("\n", space.ExamplePairs.Select(p => $"User: {p.User}\nCake: {p.Assistant}")));
        if (space.Files.Count > 0)
            parts.Add("Space files and permissions:\n" + string.Join("\n", space.Files.Select(f => $"- {f.DisplayName}: {(f.Permission == SpaceFilePermission.ReadWrite ? "read/write" : "read-only")}")));
        return string.Join("\n\n", parts);
    }
}
