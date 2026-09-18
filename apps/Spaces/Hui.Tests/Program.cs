using CakeOS.Spaces;
using CakeOS.Spaces.Hui;

var root = Path.Combine(Path.GetTempPath(), "cakeos-spaces-hui-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    var registry = new SpaceRegistry(new JsonSpaceStore(Path.Combine(root, "spaces.json")));
    var conversations = new MemoryConversationStore();
    var conversationService = new SpaceConversationService(conversations, registry);
    var shell = new RecordingShell();
    var app = new SpacesApplicationService(registry, conversationService, shell, new FixedModels(["gemma3:4b"]));
    using var controller = new SpacesHuiController(registry, conversationService, app, new FakePicker(root));

    await controller.ActivateAsync();
    Require(controller.Scene.Root is not null, "scene root exists");
    Require(controller.SelectedSpaceId is not null, "a built-in Space is selected");

    var custom = await registry.CreateAsync("Revision", "Exam prep");
    await controller.SelectAsync(custom.Id);
    controller.Scene.NameInput.Text = "Revision 2026";
    controller.Scene.InstructionsInput.Text = "Quiz me before explaining.";
    controller.Scene.ModelInput.Text = "qwen3:4b";
    var draft = controller.Scene.ReadDraft();
    await registry.UpdateAsync(custom with { Name = draft.Name, Instructions = draft.Instructions, ModelName = draft.ModelName });
    await controller.RefreshAsync();

    Require(controller.Scene.NameInput.Text == "Revision 2026", "editor reflects persisted custom Space");

    await app.LaunchAsync(custom.Id);
    Require(shell.LastTarget == "chat", "general Space launches configured chat");
    Require(shell.LastPlan?.ModelName is null, "missing preferred model falls back instead of blocking launch");
    Require((await conversationService.ListForSpaceAsync(custom.Id)).Count == 1, "launch created scoped conversation");

    await app.LaunchAsync(SpaceRegistry.StudySpaceId);
    Require(shell.LastTarget == "study", "Study routes to Study product");
    await app.LaunchAsync(SpaceRegistry.AgentSpaceId);
    Require(shell.LastTarget == "tasks", "Agent routes to Tasks");
    await app.LaunchAsync(SpaceRegistry.ResearchSpaceId);
    Require(shell.LastTarget == "chat", "Research routes to configured workspace");

    await app.OpenLayoutAsync(custom.Id);
    Require(shell.LastTarget == "layout", "Space layout routes through shared shell contract");

    var picked = await new FakePicker(root).PickFilesAsync("files");
    await registry.AddFileAsync(custom.Id, picked[0], SpaceFilePermission.ReadOnly);
    await controller.RefreshAsync();
    Require((await registry.GetAsync(custom.Id))!.Files.Count == 1, "attached file persisted");

    Console.WriteLine("CakeOS Spaces HUI/controller smoke: PASS");
}
finally
{
    try { Directory.Delete(root, true); } catch { }
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException("FAILED: " + message);
}

sealed class MemoryConversationStore : ISpaceConversationStore
{
    private readonly Dictionary<Guid, SpaceConversation> _items = [];
    public Task<IReadOnlyList<SpaceConversation>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SpaceConversation>>(_items.Values.ToArray());
    public Task<SpaceConversation?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_items.GetValueOrDefault(id));
    public Task UpsertAsync(SpaceConversation conversation, CancellationToken cancellationToken = default) { _items[conversation.Id] = conversation; return Task.CompletedTask; }
}

sealed class RecordingShell : ISpacesShellBridge
{
    public string LastTarget { get; private set; } = string.Empty;
    public SpaceLaunchPlan? LastPlan { get; private set; }
    public Task OpenHomeAsync(CancellationToken cancellationToken = default) { LastTarget = "home"; return Task.CompletedTask; }
    public Task OpenUnscopedChatAsync(CancellationToken cancellationToken = default) { LastTarget = "chat-unscoped"; return Task.CompletedTask; }
    public Task OpenStudyAsync(SpaceDefinition space, CancellationToken cancellationToken = default) { LastTarget = "study"; return Task.CompletedTask; }
    public Task OpenTasksAsync(SpaceDefinition space, CancellationToken cancellationToken = default) { LastTarget = "tasks"; return Task.CompletedTask; }
    public Task OpenConfiguredChatAsync(SpaceDefinition space, SpaceLaunchPlan plan, SpaceConversation conversation, CancellationToken cancellationToken = default) { LastTarget = "chat"; LastPlan = plan; return Task.CompletedTask; }
    public Task OpenConversationAsync(SpaceConversation conversation, CancellationToken cancellationToken = default) { LastTarget = "conversation"; return Task.CompletedTask; }
    public Task OpenSpaceLayoutAsync(SpaceDefinition space, CancellationToken cancellationToken = default) { LastTarget = "layout"; return Task.CompletedTask; }
}

sealed class FakePicker(string root) : ISpacesFilePicker
{
    public async Task<IReadOnlyList<string>> PickFilesAsync(string title, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(root, "notes.txt");
        await File.WriteAllTextAsync(path, "notes", cancellationToken);
        return [path];
    }
}

sealed class FixedModels(IReadOnlyList<string> models) : ISpacesModelCatalog
{
    public Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(models);
}
