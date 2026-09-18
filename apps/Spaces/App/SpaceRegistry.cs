namespace CakeOS.Spaces;

public sealed class SpaceRegistry
{
    private const int CurrentVersion = 1;
    private readonly ISpaceStore _store;
    private readonly Func<DateTimeOffset> _clock;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public static readonly Guid StudySpaceId = Guid.Parse("b1000000-0000-0000-0000-000000000001");
    public static readonly Guid ShoppingSpaceId = Guid.Parse("b1000000-0000-0000-0000-000000000002");
    public static readonly Guid ResearchSpaceId = Guid.Parse("b1000000-0000-0000-0000-000000000003");
    public static readonly Guid AgentSpaceId = Guid.Parse("b1000000-0000-0000-0000-000000000004");

    public SpaceRegistry(ISpaceStore store) : this(store, () => DateTimeOffset.UtcNow) { }

    internal SpaceRegistry(ISpaceStore store, Func<DateTimeOffset> clock)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<IReadOnlyList<SpaceDefinition>> GetAllAsync(bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var state = await ReadStateAsync(cancellationToken).ConfigureAwait(false);
        return state.Spaces
            .Where(space => includeArchived || !space.IsArchived)
            .OrderByDescending(space => space.IsBuiltIn)
            .ThenBy(space => space.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<SpaceDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        (await GetAllAsync(true, cancellationToken).ConfigureAwait(false)).FirstOrDefault(space => space.Id == id);

    public async Task<Guid?> GetCurrentSpaceIdAsync(CancellationToken cancellationToken = default) =>
        (await ReadStateAsync(cancellationToken).ConfigureAwait(false)).CurrentSpaceId;

    public Task SetCurrentSpaceIdAsync(Guid? id, CancellationToken cancellationToken = default) =>
        MutateAsync(state =>
        {
            if (id is not null && state.Spaces.All(space => space.Id != id))
                throw new KeyNotFoundException($"Space '{id}' was not found.");
            return (state with { CurrentSpaceId = id }, true);
        }, cancellationToken);

    public Task<SpaceDefinition> CreateAsync(string name, string? description = null, CancellationToken cancellationToken = default)
    {
        var clean = NormalizeName(name);
        return MutateAsync(state =>
        {
            EnsureUniqueName(state.Spaces, clean);
            var now = _clock();
            var created = new SpaceDefinition(
                Guid.NewGuid(), clean, description?.Trim() ?? string.Empty, "sparkles", SpaceKind.General,
                false, false, null, string.Empty, SpaceThinkingMode.Default, [], [], null, now, now);
            return (state with { Spaces = [.. state.Spaces, created] }, created);
        }, cancellationToken);
    }

    public Task<SpaceDefinition> UpdateAsync(SpaceDefinition updated, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updated);
        return MutateAsync(state =>
        {
            var existing = FindRequired(state.Spaces, updated.Id);
            var clean = NormalizeName(updated.Name);
            EnsureUniqueName(state.Spaces, clean, updated.Id);
            var safe = updated with
            {
                Name = clean,
                IsBuiltIn = existing.IsBuiltIn,
                Kind = existing.IsBuiltIn ? existing.Kind : updated.Kind,
                CreatedAt = existing.CreatedAt,
                UpdatedAt = _clock(),
                ExamplePairs = updated.ExamplePairs ?? [],
                Files = updated.Files ?? []
            };
            return (state with { Spaces = state.Spaces.Select(s => s.Id == safe.Id ? safe : s).ToArray() }, safe);
        }, cancellationToken);
    }

    public Task<SpaceDefinition> SetArchivedAsync(Guid id, bool archived, CancellationToken cancellationToken = default) =>
        MutateSpaceAsync(id, s => s with { IsArchived = archived, UpdatedAt = _clock() }, cancellationToken);

    public Task<SpaceDefinition> ForkAsync(Guid id, string? name = null, CancellationToken cancellationToken = default) =>
        MutateAsync(state =>
        {
            var source = FindRequired(state.Spaces, id);
            var desired = NormalizeName(string.IsNullOrWhiteSpace(name) ? $"{source.Name} copy" : name!);
            var unique = UniqueName(state.Spaces, desired);
            var now = _clock();
            var fork = source with
            {
                Id = Guid.NewGuid(),
                Name = unique,
                IsBuiltIn = false,
                IsArchived = false,
                ForkedFromSpaceId = source.Id,
                Files = source.Files.ToArray(),
                ExamplePairs = source.ExamplePairs.ToArray(),
                CreatedAt = now,
                UpdatedAt = now
            };
            return (state with { Spaces = [.. state.Spaces, fork] }, fork);
        }, cancellationToken);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        MutateAsync(state =>
        {
            var existing = FindRequired(state.Spaces, id);
            if (existing.IsBuiltIn)
                throw new InvalidOperationException("Built-in Spaces cannot be deleted. Fork one to create an editable copy.");
            var current = state.CurrentSpaceId == id ? null : state.CurrentSpaceId;
            return (state with { Spaces = state.Spaces.Where(space => space.Id != id).ToArray(), CurrentSpaceId = current }, true);
        }, cancellationToken);

    public Task<SpaceDefinition> AddFileAsync(Guid id, string path, SpaceFilePermission permission = SpaceFilePermission.ReadOnly, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A file path is required.", nameof(path));
        var fullPath = Path.GetFullPath(path);
        return MutateSpaceAsync(id, space =>
        {
            var files = space.Files
                .Where(file => !file.Path.Equals(fullPath, StringComparison.OrdinalIgnoreCase))
                .Append(new SpaceFileReference(fullPath, Path.GetFileName(fullPath), permission, _clock()))
                .ToArray();
            return space with { Files = files, UpdatedAt = _clock() };
        }, cancellationToken);
    }

    public Task<SpaceDefinition> RemoveFileAsync(Guid id, string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A file path is required.", nameof(path));
        var fullPath = Path.GetFullPath(path);
        return MutateSpaceAsync(id, space => space with
        {
            Files = space.Files.Where(file => !file.Path.Equals(fullPath, StringComparison.OrdinalIgnoreCase)).ToArray(),
            UpdatedAt = _clock()
        }, cancellationToken);
    }

    private Task<SpaceDefinition> MutateSpaceAsync(Guid id, Func<SpaceDefinition, SpaceDefinition> mutation, CancellationToken token) =>
        MutateAsync(state =>
        {
            var existing = FindRequired(state.Spaces, id);
            var changed = mutation(existing);
            return (state with { Spaces = state.Spaces.Select(s => s.Id == id ? changed : s).ToArray() }, changed);
        }, token);

    private async Task<SpaceState> ReadStateAsync(CancellationToken token)
    {
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try { return await LoadAndReconcileAsync(token).ConfigureAwait(false); }
        finally { _gate.Release(); }
    }

    private async Task<TResult> MutateAsync<TResult>(Func<SpaceState, (SpaceState State, TResult Result)> mutation, CancellationToken token)
    {
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            var current = await LoadAndReconcileAsync(token).ConfigureAwait(false);
            var (next, result) = mutation(current);
            await _store.SaveAsync(next, token).ConfigureAwait(false);
            return result;
        }
        finally { _gate.Release(); }
    }

    private async Task<SpaceState> LoadAndReconcileAsync(CancellationToken token)
    {
        var state = await _store.LoadAsync(token).ConfigureAwait(false) ?? new SpaceState(CurrentVersion, null, []);
        var spaces = state.Spaces?.ToList() ?? [];
        var changed = state.Version != CurrentVersion;
        foreach (var builtIn in BuiltIns())
        {
            if (spaces.Any(space => space.Id == builtIn.Id)) continue;
            spaces.Add(builtIn);
            changed = true;
        }
        var reconciled = new SpaceState(CurrentVersion, state.CurrentSpaceId, spaces);
        if (changed) await _store.SaveAsync(reconciled, token).ConfigureAwait(false);
        return reconciled;
    }

    private static IReadOnlyList<SpaceDefinition> BuiltIns()
    {
        var epoch = DateTimeOffset.UnixEpoch;
        return
        [
            new(StudySpaceId, "Study", "Organise subjects, revision material and study workflows.", "book", SpaceKind.Study, true, false, null, "Use the Study product for subject, topic, progress and assessment work.", SpaceThinkingMode.Balanced, [], [], null, epoch, epoch),
            new(ShoppingSpaceId, "Shopping", "Compare products, research options and keep buying context together.", "cart", SpaceKind.Shopping, true, false, null, "Preserve requirements, trade-offs and comparison context.", SpaceThinkingMode.Balanced, [], [], null, epoch, epoch),
            new(ResearchSpaceId, "Research", "Collect sources, files and deeper investigation in one reusable workspace.", "search", SpaceKind.Research, true, false, null, "Separate sourced facts, inference and unresolved questions.", SpaceThinkingMode.Deep, [], [], null, epoch, epoch),
            new(AgentSpaceId, "Agent", "Long-horizon task and agent work.", "agents", SpaceKind.Agent, true, false, null, "Plan, execute, verify and leave a clear handoff.", SpaceThinkingMode.Deep, [], [], null, epoch, epoch)
        ];
    }

    private static SpaceDefinition FindRequired(IReadOnlyList<SpaceDefinition> spaces, Guid id) =>
        spaces.FirstOrDefault(space => space.Id == id) ?? throw new KeyNotFoundException($"Space '{id}' was not found.");

    private static string NormalizeName(string name)
    {
        var clean = name?.Trim() ?? string.Empty;
        if (clean.Length == 0) throw new ArgumentException("A Space name is required.", nameof(name));
        if (clean.Length > 80) throw new ArgumentException("Space names can be at most 80 characters.", nameof(name));
        return clean;
    }

    private static void EnsureUniqueName(IReadOnlyList<SpaceDefinition> spaces, string name, Guid? exceptId = null)
    {
        if (spaces.Any(space => space.Id != exceptId && space.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A Space named '{name}' already exists.");
    }

    private static string UniqueName(IReadOnlyList<SpaceDefinition> spaces, string desired)
    {
        if (!spaces.Any(space => space.Name.Equals(desired, StringComparison.OrdinalIgnoreCase))) return desired;
        for (var i = 2; i < 1000; i++)
        {
            var candidate = $"{desired} {i}";
            if (!spaces.Any(space => space.Name.Equals(candidate, StringComparison.OrdinalIgnoreCase))) return candidate;
        }
        throw new InvalidOperationException("Could not create a unique Space name.");
    }
}
