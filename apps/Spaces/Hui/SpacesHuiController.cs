namespace CakeOS.Spaces.Hui;

public sealed class SpacesHuiController : IDisposable
{
    private readonly SpaceRegistry _registry;
    private readonly SpaceConversationService _conversations;
    private readonly SpacesApplicationService _application;
    private readonly ISpacesFilePicker? _files;
    private bool _includeArchived;
    private bool _disposed;

    public SpacesHuiController(
        SpaceRegistry registry,
        SpaceConversationService conversations,
        SpacesApplicationService application,
        ISpacesFilePicker? files = null,
        SpacesHuiScene? scene = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _conversations = conversations ?? throw new ArgumentNullException(nameof(conversations));
        _application = application ?? throw new ArgumentNullException(nameof(application));
        _files = files;
        Scene = scene ?? new SpacesHuiScene();

        Scene.CreateRequested += OnCreate;
        Scene.SpaceSelected += OnSelect;
        Scene.SaveRequested += OnSave;
        Scene.LaunchRequested += OnLaunch;
        Scene.NewConversationRequested += OnNewConversation;
        Scene.ForkRequested += OnFork;
        Scene.ArchiveRequested += OnArchive;
        Scene.DeleteRequested += OnDelete;
        Scene.AddFileRequested += OnAddFile;
        Scene.RemoveFileRequested += OnRemoveFile;
        Scene.ConversationSelected += OnConversation;
        Scene.DestinationRequested += OnDestination;
    }

    public SpacesHuiScene Scene { get; }
    public Guid? SelectedSpaceId { get; private set; }

    public async Task ActivateAsync(CancellationToken token = default) => await RefreshAsync(token).ConfigureAwait(false);

    public async Task RefreshAsync(CancellationToken token = default)
    {
        var spaces = await _registry.GetAllAsync(_includeArchived, token).ConfigureAwait(false);
        if (SelectedSpaceId is null || spaces.All(s => s.Id != SelectedSpaceId))
            SelectedSpaceId = spaces.FirstOrDefault(s => !s.IsArchived)?.Id ?? spaces.FirstOrDefault()?.Id;
        Scene.SetSpaces(spaces, SelectedSpaceId);
        var current = SelectedSpaceId is { } id ? spaces.FirstOrDefault(s => s.Id == id) : null;
        Scene.SetSpace(current);
        await RefreshConversationsAsync(token).ConfigureAwait(false);
    }

    public async Task SelectAsync(Guid id, CancellationToken token = default)
    {
        SelectedSpaceId = id;
        await RefreshAsync(token).ConfigureAwait(false);
    }

    public void SetIncludeArchived(bool includeArchived) => _includeArchived = includeArchived;

    private async Task RefreshConversationsAsync(CancellationToken token)
    {
        if (SelectedSpaceId is not { } id) { Scene.SetConversations([]); return; }
        Scene.SetConversations(await _conversations.ListForSpaceAsync(id, token).ConfigureAwait(false));
    }

    private async void OnCreate(object? sender, EventArgs e) => await Safe(async () =>
    {
        var all = await _registry.GetAllAsync(true);
        var names = all.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var name = "New Space";
        for (var i = 2; names.Contains(name); i++) name = $"New Space {i}";
        var created = await _registry.CreateAsync(name, "Describe what you want this Space to help with.");
        SelectedSpaceId = created.Id;
        await RefreshAsync();
    }, "create Space");

    private async void OnSelect(object? sender, Guid id) => await Safe(() => SelectAsync(id), "select Space");

    private async void OnSave(object? sender, SpaceEditorDraft draft) => await Safe(async () =>
    {
        var current = SelectedSpaceId is { } id ? await _registry.GetAsync(id) : null;
        if (current is null) return;
        await _registry.UpdateAsync(current with
        {
            Name = draft.Name,
            Description = draft.Description,
            ModelName = draft.ModelName,
            Instructions = draft.Instructions,
            ThinkingMode = draft.ThinkingMode,
            ExamplePairs = draft.Examples,
            GeneratedSurface = draft.GeneratedSurface
        });
        await RefreshAsync();
        Scene.SetStatus("Space saved.");
    }, "save Space");

    private async void OnLaunch(object? sender, Guid id) => await Safe(() => _application.LaunchAsync(id), "open Space");
    private async void OnNewConversation(object? sender, Guid id) => await Safe(async () => { await _application.StartNewChatAsync(id); await RefreshConversationsAsync(CancellationToken.None); }, "start Space chat");
    private async void OnConversation(object? sender, Guid id) => await Safe(() => _application.OpenConversationAsync(id), "open Space chat");

    private async void OnFork(object? sender, Guid id) => await Safe(async () =>
    {
        var fork = await _registry.ForkAsync(id);
        SelectedSpaceId = fork.Id;
        await RefreshAsync();
        Scene.SetStatus($"Forked as {fork.Name}.");
    }, "fork Space");

    private async void OnArchive(object? sender, Guid id) => await Safe(async () =>
    {
        var current = await _registry.GetAsync(id);
        if (current is null) return;
        await _registry.SetArchivedAsync(id, !current.IsArchived);
        if (!current.IsArchived && !_includeArchived) SelectedSpaceId = null;
        await RefreshAsync();
    }, "archive or restore Space");

    private async void OnDelete(object? sender, Guid id) => await Safe(async () =>
    {
        await _application.DeleteCustomSpaceAsync(id);
        if (SelectedSpaceId == id) SelectedSpaceId = null;
        await RefreshAsync();
        Scene.SetStatus("Custom Space deleted; its conversations were detached.");
    }, "delete Space");

    private async void OnAddFile(object? sender, SpaceFilePermission permission) => await Safe(async () =>
    {
        if (_files is null || SelectedSpaceId is not { } id) { Scene.SetStatus("File picker is unavailable in this host."); return; }
        var picked = await _files.PickFilesAsync("Add files to Space");
        foreach (var path in picked) await _registry.AddFileAsync(id, path, permission);
        await RefreshAsync();
    }, "add files");

    private async void OnRemoveFile(object? sender, string path) => await Safe(async () =>
    {
        if (SelectedSpaceId is not { } id) return;
        await _registry.RemoveFileAsync(id, path);
        await RefreshAsync();
    }, "remove file");

    private async void OnDestination(object? sender, SpacesDestination destination) => await Safe(async () =>
    {
        switch (destination)
        {
            case SpacesDestination.Home: await _application.OpenHomeAsync(); break;
            case SpacesDestination.Chat: await _application.OpenChatAsync(); break;
            case SpacesDestination.Study: await _application.LaunchAsync(SpaceRegistry.StudySpaceId); break;
            case SpacesDestination.Tasks: await _application.LaunchAsync(SpaceRegistry.AgentSpaceId); break;
            case SpacesDestination.Research: await _application.LaunchAsync(SpaceRegistry.ResearchSpaceId); break;
            default: throw new ArgumentOutOfRangeException(nameof(destination));
        }
    }, "navigate");

    private async Task Safe(Func<Task> operation, string action)
    {
        if (_disposed) return;
        try { await operation().ConfigureAwait(false); }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException or UnauthorizedAccessException or KeyNotFoundException)
        { Scene.SetStatus($"Could not {action}: {ex.Message}"); }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Scene.CreateRequested -= OnCreate;
        Scene.SpaceSelected -= OnSelect;
        Scene.SaveRequested -= OnSave;
        Scene.LaunchRequested -= OnLaunch;
        Scene.NewConversationRequested -= OnNewConversation;
        Scene.ForkRequested -= OnFork;
        Scene.ArchiveRequested -= OnArchive;
        Scene.DeleteRequested -= OnDelete;
        Scene.AddFileRequested -= OnAddFile;
        Scene.RemoveFileRequested -= OnRemoveFile;
        Scene.ConversationSelected -= OnConversation;
        Scene.DestinationRequested -= OnDestination;
    }
}
