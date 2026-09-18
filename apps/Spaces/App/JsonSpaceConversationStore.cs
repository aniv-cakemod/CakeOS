using System.Text.Json;

namespace CakeOS.Spaces;

public sealed class JsonSpaceConversationStore : ISpaceConversationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonSpaceConversationStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A conversation persistence path is required.", nameof(path));
        _path = Path.GetFullPath(path);
    }

    public async Task<IReadOnlyList<SpaceConversation>> ListAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { return await LoadCoreAsync(cancellationToken).ConfigureAwait(false); }
        finally { _gate.Release(); }
    }

    public async Task<SpaceConversation?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        (await ListAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault(item => item.Id == id);

    public async Task UpsertAsync(SpaceConversation conversation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var items = (await LoadCoreAsync(cancellationToken).ConfigureAwait(false)).ToList();
            var index = items.FindIndex(item => item.Id == conversation.Id);
            if (index >= 0) items[index] = conversation;
            else items.Add(conversation);
            await SaveCoreAsync(items, cancellationToken).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    private async Task<IReadOnlyList<SpaceConversation>> LoadCoreAsync(CancellationToken token)
    {
        if (!File.Exists(_path)) return [];
        await using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        return await JsonSerializer.DeserializeAsync<SpaceConversation[]>(stream, JsonOptions, token).ConfigureAwait(false) ?? [];
    }

    private async Task SaveCoreAsync(IReadOnlyList<SpaceConversation> items, CancellationToken token)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        var temporary = _path + ".tmp";
        await using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
        {
            await JsonSerializer.SerializeAsync(stream, items, JsonOptions, token).ConfigureAwait(false);
            await stream.FlushAsync(token).ConfigureAwait(false);
        }
        File.Move(temporary, _path, overwrite: true);
    }
}
