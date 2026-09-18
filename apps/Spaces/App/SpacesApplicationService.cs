namespace CakeOS.Spaces;

public interface ISpacesShellBridge
{
    Task OpenHomeAsync(CancellationToken cancellationToken = default);
    Task OpenUnscopedChatAsync(CancellationToken cancellationToken = default);
    Task OpenStudyAsync(SpaceDefinition space, CancellationToken cancellationToken = default);
    Task OpenTasksAsync(SpaceDefinition space, CancellationToken cancellationToken = default);
    Task OpenConfiguredChatAsync(SpaceDefinition space, SpaceLaunchPlan plan, SpaceConversation conversation, CancellationToken cancellationToken = default);
    Task OpenConversationAsync(SpaceConversation conversation, CancellationToken cancellationToken = default);
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

    public SpacesApplicationService(SpaceRegistry registry, SpaceConversationService conversations, ISpacesShellBridge shell)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _conversations = conversations ?? throw new ArgumentNullException(nameof(conversations));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
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
        var plan = SpaceLaunchPolicy.Resolve(space);
        if (plan.Target == SpaceLaunchTarget.Study)
        {
            await _shell.OpenStudyAsync(space, token).ConfigureAwait(false);
            return;
        }
        if (plan.Target == SpaceLaunchTarget.Tasks)
        {
            await _shell.OpenTasksAsync(space, token).ConfigureAwait(false);
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
        await _conversations.DetachSpaceAsync(spaceId, token).ConfigureAwait(false);
        await _registry.DeleteAsync(spaceId, token).ConfigureAwait(false);
    }
}
