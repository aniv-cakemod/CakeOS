using CakeOS.Spaces;

var root = Path.Combine(Path.GetTempPath(), "cakeos-spaces-smoke-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
var path = Path.Combine(root, "spaces.json");

try
{
    var registry = new SpaceRegistry(new JsonSpaceStore(path));
    var builtIns = await registry.GetAllAsync();
    Require(builtIns.Count(s => s.IsBuiltIn) == 4, "four built-in Spaces");
    Require(builtIns.Any(s => s.Id == SpaceRegistry.StudySpaceId), "Study built-in");

    var created = await registry.CreateAsync("Revision");
    var edited = await registry.UpdateAsync(created with { Description = "GCSE revision", Instructions = "Prefer concise quizzes." });
    Require(edited.Description == "GCSE revision", "edit persisted");

    var file = Path.Combine(root, "notes.txt");
    await File.WriteAllTextAsync(file, "biology notes");
    var withFile = await registry.AddFileAsync(created.Id, file, SpaceFilePermission.ReadOnly);
    Require(withFile.Files.Count == 1, "file attached");

    await registry.SetCurrentSpaceIdAsync(created.Id);
    var reopened = new SpaceRegistry(new JsonSpaceStore(path));
    Require((await reopened.GetAsync(created.Id)) is not null, "custom Space survived reopen");
    Require(await reopened.GetCurrentSpaceIdAsync() == created.Id, "current scope survived reopen");

    var fork = await reopened.ForkAsync(SpaceRegistry.StudySpaceId);
    Require(!fork.IsBuiltIn && fork.ForkedFromSpaceId == SpaceRegistry.StudySpaceId, "built-in fork");

    await reopened.SetArchivedAsync(created.Id, true);
    Require((await reopened.GetAllAsync()).All(s => s.Id != created.Id), "archive hidden");
    Require((await reopened.GetAllAsync(true)).Any(s => s.Id == created.Id && s.IsArchived), "archive recoverable");
    await reopened.SetArchivedAsync(created.Id, false);

    var conversations = new MemoryConversationStore();
    var service = new SpaceConversationService(conversations, reopened);
    var chat = await service.StartAsync(created.Id);
    Require(chat.SpaceId == created.Id, "new chat assigned to Space");
    Require((await service.ListForSpaceAsync(created.Id)).Count == 1, "Space conversation listed");
    await service.DetachSpaceAsync(created.Id);
    Require((await conversations.GetAsync(chat.Id))?.SpaceId is null, "delete safety detaches conversations");

    await reopened.DeleteAsync(created.Id);
    Require(await reopened.GetAsync(created.Id) is null, "custom Space deleted");

    var builtInDeleteRejected = false;
    try { await reopened.DeleteAsync(SpaceRegistry.StudySpaceId); }
    catch (InvalidOperationException) { builtInDeleteRejected = true; }
    Require(builtInDeleteRejected, "built-in delete rejected");

    Console.WriteLine("CakeOS Spaces domain/persistence smoke: PASS");
}
finally
{
    try { Directory.Delete(root, recursive: true); } catch { }
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException("FAILED: " + message);
}

sealed class MemoryConversationStore : ISpaceConversationStore
{
    private readonly Dictionary<Guid, SpaceConversation> _items = [];
    public Task<IReadOnlyList<SpaceConversation>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SpaceConversation>>(_items.Values.ToArray());
    public Task<SpaceConversation?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));
    public Task UpsertAsync(SpaceConversation conversation, CancellationToken cancellationToken = default)
    {
        _items[conversation.Id] = conversation;
        return Task.CompletedTask;
    }
}
