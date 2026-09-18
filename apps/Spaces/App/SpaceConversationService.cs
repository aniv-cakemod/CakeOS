namespace CakeOS.Spaces;

public interface ISpaceConversationStore
{
    Task<IReadOnlyList<SpaceConversation>> ListAsync(CancellationToken cancellationToken = default);
    Task<SpaceConversation?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpsertAsync(SpaceConversation conversation, CancellationToken cancellationToken = default);
}

public sealed class SpaceConversationService
{
    private readonly ISpaceConversationStore _conversations;
    private readonly SpaceRegistry _spaces;
    private readonly Func<DateTimeOffset> _clock;

    public SpaceConversationService(ISpaceConversationStore conversations, SpaceRegistry spaces)
        : this(conversations, spaces, () => DateTimeOffset.UtcNow) { }

    internal SpaceConversationService(ISpaceConversationStore conversations, SpaceRegistry spaces, Func<DateTimeOffset> clock)
    {
        _conversations = conversations ?? throw new ArgumentNullException(nameof(conversations));
        _spaces = spaces ?? throw new ArgumentNullException(nameof(spaces));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<IReadOnlyList<SpaceConversation>> ListForSpaceAsync(Guid spaceId, CancellationToken token = default) =>
        (await _conversations.ListAsync(token).ConfigureAwait(false))
            .Where(c => !c.IsArchived && c.SpaceId == spaceId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToArray();

    public async Task<SpaceConversation> StartAsync(Guid spaceId, string title = "New chat", CancellationToken token = default)
    {
        _ = await _spaces.GetAsync(spaceId, token).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Space '{spaceId}' was not found.");
        var conversation = new SpaceConversation(Guid.NewGuid(), title, spaceId, _clock());
        await _conversations.UpsertAsync(conversation, token).ConfigureAwait(false);
        await _spaces.SetCurrentSpaceIdAsync(spaceId, token).ConfigureAwait(false);
        return conversation;
    }

    public async Task<SpaceConversation?> OpenMostRecentAsync(Guid spaceId, CancellationToken token = default)
    {
        var recent = (await ListForSpaceAsync(spaceId, token).ConfigureAwait(false)).FirstOrDefault();
        if (recent is not null) await _spaces.SetCurrentSpaceIdAsync(spaceId, token).ConfigureAwait(false);
        return recent;
    }

    public async Task DetachSpaceAsync(Guid spaceId, CancellationToken token = default)
    {
        var all = await _conversations.ListAsync(token).ConfigureAwait(false);
        foreach (var conversation in all.Where(c => c.SpaceId == spaceId))
            await _conversations.UpsertAsync(conversation with { SpaceId = null, UpdatedAt = _clock() }, token).ConfigureAwait(false);
    }
}
