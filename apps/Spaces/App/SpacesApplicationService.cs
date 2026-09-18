namespace CakeOS.Spaces;

public interface ISpacesShellBridge
{
    Task OpenHomeAsync(CancellationToken cancellationToken = default);
    Task OpenUnscopedChatAsync(CancellationToken cancellationToken = default);
    Task OpenStudyAsync(SpaceDefinition space, SpaceLaunchPlan plan, CancellationToken cancellationToken = default);
    Task OpenTasksAsync(SpaceDefinition space, SpaceLaunchPlan plan, CancellationToken cancellationToken = default);
    Task OpenConfiguredChatAsync(SpaceDefinition space, SpaceLaunchPlan plan, SpaceConversation conversation, CancellationToken cancellationToken = default);
    Task OpenConversationAsync(SpaceConversation conversation, CancellationToken cancellationToken = default);
    Task OpenSpaceLayoutAsync(SpaceDefinition space, CancellationToken cancellationToken = default);
}

public interface ISpacesFilePicker
{
    Task<IReadOnlyList<string>> PickFilesAsync(string title, CancellationToken cancellationToken = default);
}

public interface ISpacesModelCatalog
{
    Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
}

public sealed class SpacesApplicationService
{
    private readonly SpaceRegistry _registry;
    private readonly SpaceConversationService _conversations;
    private readonly ISpacesShellBridge _shell;
    private readonly ISpacesModelCatalog? _models;

    public SpacesApplicationService(
        SpaceRegistry registry,
        SpaceConversationService conversations,
        ISpacesShellBridge shell,
        ISpacesModelCatalog? models = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _conversations = conversations ?? throw new ArgumentNullException(nameof(conversations));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _models = models;
    }

    public async Task OpenHomeAsync(CancellationToken token = default)
    {
        await _registry.SetCurrentSpaceIdAsync(null, token).ConfigureAwait(false);
        await _shell.OpenHomeAsync(token).ConfigureAwait(false);
    }

    public async Task OpenChatAsync(CancellationToken token = default)
    {
        await _registry.SetCurrentSpaceIdAsync(null, token).ConfigureAwait(false);
        await _shell.OpenUnscopedChatAsync(token).ConfigureAwait(false);
    }

    public async Task LaunchAsync(Guid spaceId, CancellationToken token = default)
    {
        var space = await _registry.GetAsync(spaceId, token).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Space '{spaceId}' was not found.");
        if (space.IsArchived) throw new InvalidOperationException("Archived Spaces must be restored before opening.");

        await _registry.SetCurrentSpaceIdAsync(space.Id, token).ConfigureAwait(false);
        var plan = await ResolveAvailableModelAsync(SpaceLaunchPolicy.Resolve(space), token).ConfigureAwait(false);
        if (plan.Target == SpaceLaunchTarget.Study)
        {
            await _shell.OpenStudyAsync(space, plan, token).ConfigureAwait(false);
            return;
        }
        if (plan.Target == SpaceLaunchTarget.Tasks)
        {
            await _shell.OpenTasksAsync(space, plan, token).ConfigureAwait(false);
            return;
        }

        var conversation = await _conversations.OpenMostRecentAsync(space.Id, token).ConfigureAwait(false)
            ?? await _conversations.StartAsync(space.Id, token: token).ConfigureAwait(false);
        await _shell.OpenConfiguredChatAsync(space, plan, conversation, token).ConfigureAwait(false);
    }

    public async Task<SpaceConversation> StartNewChatAsync(Guid spaceId, CancellationToken token = default)
    {
        var conversation = await _conversations.StartAsync(spaceId, token: token).ConfigureAwait(false);
        await _shell.OpenConversationAsync(conversation, token).ConfigureAwait(false);
        return conversation;
    }

    public async Task OpenConversationAsync(Guid conversationId, CancellationToken token = default)
    {
        var conversation = await _conversations.GetAsync(conversationId, token).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Conversation '{conversationId}' was not found.");
        if (conversation.SpaceId is { } spaceId)
            await _registry.SetCurrentSpaceIdAsync(spaceId, token).ConfigureAwait(false);
        await _shell.OpenConversationAsync(conversation, token).ConfigureAwait(false);
    }

    public async Task DeleteCustomSpaceAsync(Guid spaceId, CancellationToken token = default)
    {
        var space = await _registry.GetAsync(spaceId, token).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Space '{spaceId}' was not found.");
        if (space.IsBuiltIn)
            throw new InvalidOperationException("Built-in Spaces cannot be deleted. Fork one to create an independent version.");

        await _conversations.DetachSpaceAsync(spaceId, token).ConfigureAwait(false);
        await _registry.DeleteAsync(spaceId, token).ConfigureAwait(false);
    }

    public async Task OpenLayoutAsync(Guid spaceId, CancellationToken token = default)
    {
        var space = await _registry.GetAsync(spaceId, token).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Space '{spaceId}' was not found.");
        await _shell.OpenSpaceLayoutAsync(space, token).ConfigureAwait(false);
    }

    private async Task<SpaceLaunchPlan> ResolveAvailableModelAsync(SpaceLaunchPlan plan, CancellationToken token)
    {
        if (_models is null || string.IsNullOrWhiteSpace(plan.ModelName)) return plan;
        try
        {
            var available = await _models.GetAvailableModelsAsync(token).ConfigureAwait(false);
            return available.Any(model => model.Equals(plan.ModelName, StringComparison.OrdinalIgnoreCase))
                ? plan
                : plan with { ModelName = null };
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
            return plan with { ModelName = null };
        }
    }
}
