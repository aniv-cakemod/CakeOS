namespace CakeOS.Spaces;

public static class SpacesPlatformPaths
{
    public static string GetDefaultStateDirectory()
    {
        var xdgStateHome = Environment.GetEnvironmentVariable("XDG_STATE_HOME");
        if (!string.IsNullOrWhiteSpace(xdgStateHome))
            return Path.Combine(Path.GetFullPath(xdgStateHome), "cakeos", "spaces");

        if (OperatingSystem.IsLinux())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(home))
                return Path.Combine(home, ".local", "state", "cakeos", "spaces");
        }

        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(local))
            local = Path.GetTempPath();
        return Path.Combine(local, "CakeOS", "Spaces");
    }

    public static SpaceRegistry CreateDefaultRegistry()
    {
        var root = GetDefaultStateDirectory();
        return new SpaceRegistry(new JsonSpaceStore(Path.Combine(root, "spaces.json")));
    }

    public static JsonSpaceConversationStore CreateDefaultConversationStore()
    {
        var root = GetDefaultStateDirectory();
        return new JsonSpaceConversationStore(Path.Combine(root, "conversations.json"));
    }
}
