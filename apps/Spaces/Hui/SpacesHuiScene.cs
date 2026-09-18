using CakeOS.Platform;
using Haven.UI;
using Haven.UI.Components;
using HuiButton = Haven.UI.Components.Button;
using HuiText = Haven.UI.Components.Text;

namespace CakeOS.Spaces.Hui;

public enum SpacesDestination { Home, Chat, Study, Tasks, Research }

public sealed record SpaceEditorDraft(
    string Name,
    string Description,
    string? ModelName,
    string Instructions,
    SpaceThinkingMode ThinkingMode,
    IReadOnlyList<SpaceExamplePair> Examples,
    SpaceGeneratedSurface? GeneratedSurface);

public static class SpacesHuiProduct
{
    public const string ProductId = "cakeos.spaces";
    public const string Entrypoint = "CakeOS.Spaces.Hui.SpacesHuiProduct";

    public static ProductRegistration CreateRegistration() => new(
        ProductId,
        "Spaces",
        ProductType.App,
        "/apps/spaces",
        _ => new SpacesHuiRootElement(new SpacesHuiScene().Root),
        Entrypoint,
        new ProductCapabilities(true, false, false, false, ["spaces", "chat-scope"]),
        new ProductDependencies([], [], []),
        new ProductPersistence(true, false, true, true),
        new ProductPermissions(["files.read"], ["files.write"]),
        [],
        [AppLifecycleOperation.Create, AppLifecycleOperation.Activate, AppLifecycleOperation.Open, AppLifecycleOperation.Suspend, AppLifecycleOperation.Resume, AppLifecycleOperation.RequestClose, AppLifecycleOperation.Recover],
        LaunchAvailability.Always);
}

public sealed class SpacesHuiRootElement(Page root) : IHuiRootElement
{
    public Page Root { get; } = root ?? throw new ArgumentNullException(nameof(root));
    public object NativeRoot => Root;
}

public sealed class SpacesHuiScene
{
    private readonly Dictionary<Guid, HuiButton> _spaceButtons = [];
    private readonly Dictionary<Guid, HuiButton> _conversationButtons = [];
    private Guid? _selectedSpaceId;

    public SpacesHuiScene()
    {
        Root = new Page { Name = "Spaces.Root", Layout = HavenLayout.Grid, Columns = "232px 1fr", Rows = "Auto 1fr Auto" };
        Root.SetValue(HavenProperties.Width, HavenLength.Percent(100));
        Root.SetValue(HavenProperties.Height, HavenLength.Percent(100));
        Root.SetValue(HavenProperties.Background, "Surface");

        Header = new HeaderBar { Name = "Spaces.Header", Title = "Spaces", Subtitle = "Reusable working contexts for Cake", IconKey = "sparkles" };
        Header.SetValue(HavenProperties.ColumnSpan, 2);
        Header.SetValue(HavenProperties.Row, 0);
        CreateButton = Action("Spaces.Create", "New Space", ButtonVariant.Primary);
        Header.AddAction(CreateButton);
        Root.Add(Header);

        Sidebar = new Sidebar { Name = "Spaces.Sidebar" };
        Sidebar.SetValue(HavenProperties.Row, 1);
        Sidebar.SetValue(HavenProperties.Column, 0);
        Sidebar.SetItems([
            new SidebarItem("home", "Home", "home"),
            new SidebarItem("chat", "Chat", "chat"),
            new SidebarItem("study", "Study", "book"),
            new SidebarItem("tasks", "Tasks", "tasks"),
            new SidebarItem("research", "Research", "search")
        ]);
        Root.Add(Sidebar);

        Main = new Container { Name = "Spaces.Main", Layout = HavenLayout.Grid, Columns = "minmax(220px, 320px) 1fr", Rows = "1fr" };
        Main.SetValue(HavenProperties.Row, 1);
        Main.SetValue(HavenProperties.Column, 1);
        Main.SetValue(HavenProperties.Padding, HavenThickness.Parse("16px"));
        Main.SetValue(HavenProperties.Gap, HavenLength.Px(16));
        Root.Add(Main);

        PickerPanel = new Panel { Name = "Spaces.PickerPanel", Title = "Your Spaces", IsCollapsible = false };
        PickerPanel.SetValue(HavenProperties.Column, 0);
        SpacesList = new Container { Name = "Spaces.List", Layout = HavenLayout.Vertical };
        SpacesList.SetValue(HavenProperties.Gap, HavenLength.Px(6));
        PickerPanel.SetContent(SpacesList);
        Main.Add(PickerPanel);

        DetailPanel = new Panel { Name = "Spaces.DetailPanel", Title = "Space details", IsCollapsible = false };
        DetailPanel.SetValue(HavenProperties.Column, 1);
        Editor = new Container { Name = "Spaces.Editor", Layout = HavenLayout.Vertical };
        Editor.SetValue(HavenProperties.Gap, HavenLength.Px(8));

        NameInput = NewInput("Spaces.Editor.Name", "Space name");
        DescriptionInput = NewInput("Spaces.Editor.Description", "Description", true);
        ModelInput = NewInput("Spaces.Editor.Model", "Preferred model (optional)");
        InstructionsInput = NewInput("Spaces.Editor.Instructions", "Instructions", true);
        ExampleUserInput = NewInput("Spaces.Editor.ExampleUser", "Example user message");
        ExampleAssistantInput = NewInput("Spaces.Editor.ExampleAssistant", "Example Cake response");
        SurfaceTemplateInput = NewInput("Spaces.Editor.SurfaceTemplate", "Generated surface template key");
        SurfaceInputsInput = NewInput("Spaces.Editor.SurfaceInputs", "Generated surface inputs JSON", true);

        foreach (var input in new[] { NameInput, DescriptionInput, ModelInput, InstructionsInput, ExampleUserInput, ExampleAssistantInput, SurfaceTemplateInput, SurfaceInputsInput })
            Editor.Add(input);

        ThinkingRow = new Container { Name = "Spaces.Editor.Thinking", Layout = HavenLayout.Horizontal };
        ThinkingRow.SetValue(HavenProperties.Gap, HavenLength.Px(6));
        foreach (var mode in Enum.GetValues<SpaceThinkingMode>())
        {
            var button = Action($"Spaces.Thinking.{mode}", mode.ToString(), ButtonVariant.Secondary);
            button.Invoked += (_, _) => { ThinkingMode = mode; SetStatus($"Thinking mode: {mode}"); };
            ThinkingRow.Add(button);
        }
        Editor.Add(ThinkingRow);

        ActionRow = new Container { Name = "Spaces.Actions", Layout = HavenLayout.Horizontal };
        ActionRow.SetValue(HavenProperties.Gap, HavenLength.Px(6));
        SaveButton = Action("Spaces.Save", "Save", ButtonVariant.Primary);
        LaunchButton = Action("Spaces.Launch", "Open", ButtonVariant.Secondary);
        NewChatButton = Action("Spaces.NewChat", "New chat", ButtonVariant.Secondary);
        ForkButton = Action("Spaces.Fork", "Fork", ButtonVariant.Secondary);
        ArchiveButton = Action("Spaces.Archive", "Archive", ButtonVariant.Secondary);
        DeleteButton = Action("Spaces.Delete", "Delete", ButtonVariant.Ghost);
        foreach (var button in new[] { SaveButton, LaunchButton, NewChatButton, ForkButton, ArchiveButton, DeleteButton }) ActionRow.Add(button);
        Editor.Add(ActionRow);

        FilesPanel = new Panel { Name = "Spaces.Files", Title = "Files", IsCollapsible = true };
        FilesList = new Container { Name = "Spaces.Files.List", Layout = HavenLayout.Vertical };
        FilesPanel.SetContent(FilesList);
        var fileActions = new Container { Name = "Spaces.Files.Actions", Layout = HavenLayout.Horizontal };
        fileActions.SetValue(HavenProperties.Gap, HavenLength.Px(6));
        AddReadOnlyFileButton = Action("Spaces.File.ReadOnly", "Add read-only", ButtonVariant.Secondary);
        AddReadWriteFileButton = Action("Spaces.File.ReadWrite", "Add read/write", ButtonVariant.Secondary);
        fileActions.Add(AddReadOnlyFileButton); fileActions.Add(AddReadWriteFileButton);
        Editor.Add(fileActions);
        Editor.Add(FilesPanel);

        ConversationsPanel = new Panel { Name = "Spaces.Conversations", Title = "Space conversations", IsCollapsible = true };
        ConversationsList = new Container { Name = "Spaces.Conversations.List", Layout = HavenLayout.Vertical };
        ConversationsPanel.SetContent(ConversationsList);
        Editor.Add(ConversationsPanel);

        DetailPanel.SetContent(Editor);
        Main.Add(DetailPanel);

        StatusText = new HuiText("Loading Spaces…") { Name = "Spaces.Status", Level = TextLevel.Caption };
        StatusText.SetValue(HavenProperties.Row, 2);
        StatusText.SetValue(HavenProperties.ColumnSpan, 2);
        StatusText.SetValue(HavenProperties.Padding, HavenThickness.Parse("8px 16px"));
        Root.Add(StatusText);

        CreateButton.Invoked += (_, _) => CreateRequested?.Invoke(this, EventArgs.Empty);
        Sidebar.ItemInvoked += (_, key) => DestinationRequested?.Invoke(this, key switch {
            "home" => SpacesDestination.Home,
            "chat" => SpacesDestination.Chat,
            "study" => SpacesDestination.Study,
            "tasks" => SpacesDestination.Tasks,
            "research" => SpacesDestination.Research,
            _ => SpacesDestination.Home
        });
        SaveButton.Invoked += (_, _) => SaveRequested?.Invoke(this, ReadDraft());
        LaunchButton.Invoked += (_, _) => InvokeSelected(LaunchRequested);
        NewChatButton.Invoked += (_, _) => InvokeSelected(NewConversationRequested);
        ForkButton.Invoked += (_, _) => InvokeSelected(ForkRequested);
        ArchiveButton.Invoked += (_, _) => InvokeSelected(ArchiveRequested);
        DeleteButton.Invoked += (_, _) => InvokeSelected(DeleteRequested);
        AddReadOnlyFileButton.Invoked += (_, _) => AddFileRequested?.Invoke(this, SpaceFilePermission.ReadOnly);
        AddReadWriteFileButton.Invoked += (_, _) => AddFileRequested?.Invoke(this, SpaceFilePermission.ReadWrite);

        Root.ValidateUniqueNames();
    }

    public event EventHandler? CreateRequested;
    public event EventHandler<SpacesDestination>? DestinationRequested;
    public event EventHandler<Guid>? SpaceSelected;
    public event EventHandler<SpaceEditorDraft>? SaveRequested;
    public event EventHandler<Guid>? LaunchRequested;
    public event EventHandler<Guid>? NewConversationRequested;
    public event EventHandler<Guid>? ForkRequested;
    public event EventHandler<Guid>? ArchiveRequested;
    public event EventHandler<Guid>? DeleteRequested;
    public event EventHandler<SpaceFilePermission>? AddFileRequested;
    public event EventHandler<string>? RemoveFileRequested;
    public event EventHandler<Guid>? ConversationSelected;

    public Page Root { get; }
    public HeaderBar Header { get; }
    public Sidebar Sidebar { get; }
    public Container Main { get; }
    public Panel PickerPanel { get; }
    public Panel DetailPanel { get; }
    public Container SpacesList { get; }
    public Container Editor { get; }
    public Container ThinkingRow { get; }
    public Container ActionRow { get; }
    public Panel FilesPanel { get; }
    public Container FilesList { get; }
    public Panel ConversationsPanel { get; }
    public Container ConversationsList { get; }
    public Input NameInput { get; }
    public Input DescriptionInput { get; }
    public Input ModelInput { get; }
    public Input InstructionsInput { get; }
    public Input ExampleUserInput { get; }
    public Input ExampleAssistantInput { get; }
    public Input SurfaceTemplateInput { get; }
    public Input SurfaceInputsInput { get; }
    public HuiButton CreateButton { get; }
    public HuiButton SaveButton { get; }
    public HuiButton LaunchButton { get; }
    public HuiButton NewChatButton { get; }
    public HuiButton ForkButton { get; }
    public HuiButton ArchiveButton { get; }
    public HuiButton DeleteButton { get; }
    public HuiButton AddReadOnlyFileButton { get; }
    public HuiButton AddReadWriteFileButton { get; }
    public HuiText StatusText { get; }
    public SpaceThinkingMode ThinkingMode { get; private set; } = SpaceThinkingMode.Default;

    public void SetCompact(bool compact)
    {
        Sidebar.IsCollapsed = compact;
        Main.Columns = compact ? "1fr" : "minmax(220px, 320px) 1fr";
        PickerPanel.SetValue(HavenProperties.Visibility, compact ? HavenVisibility.Collapsed : HavenVisibility.Visible);
    }

    public void SetSpaces(IReadOnlyList<SpaceDefinition> spaces, Guid? selectedId)
    {
        _selectedSpaceId = selectedId;
        foreach (var child in SpacesList.Children.ToArray()) SpacesList.Remove(child);
        _spaceButtons.Clear();
        foreach (var space in spaces)
        {
            var button = Action($"Spaces.Space.{space.Id:N}", space.IsArchived ? $"{space.Name} · archived" : space.Name, ButtonVariant.Navigation);
            button.Accessibility.AccessibleName = $"{space.Name}{(space.IsBuiltIn ? ", built-in" : ", custom")}{(space.IsArchived ? ", archived" : string.Empty)}";
            button.SetState(HavenElementState.Selected, space.Id == selectedId);
            var id = space.Id;
            button.Invoked += (_, _) => SpaceSelected?.Invoke(this, id);
            SpacesList.Add(button);
            _spaceButtons[id] = button;
        }
    }

    public void SetSpace(SpaceDefinition? space)
    {
        _selectedSpaceId = space?.Id;
        if (space is null)
        {
            NameInput.Text = DescriptionInput.Text = ModelInput.Text = InstructionsInput.Text = string.Empty;
            ExampleUserInput.Text = ExampleAssistantInput.Text = SurfaceTemplateInput.Text = SurfaceInputsInput.Text = string.Empty;
            SetStatus("Select a Space.");
            return;
        }

        NameInput.Text = space.Name;
        DescriptionInput.Text = space.Description;
        ModelInput.Text = space.ModelName ?? string.Empty;
        InstructionsInput.Text = space.Instructions;
        ThinkingMode = space.ThinkingMode;
        ExampleUserInput.Text = space.ExamplePairs.FirstOrDefault()?.User ?? string.Empty;
        ExampleAssistantInput.Text = space.ExamplePairs.FirstOrDefault()?.Assistant ?? string.Empty;
        SurfaceTemplateInput.Text = space.GeneratedSurface?.TemplateKey ?? string.Empty;
        SurfaceInputsInput.Text = space.GeneratedSurface?.InputsJson ?? string.Empty;
        ArchiveButton.Content = space.IsArchived ? "Restore" : "Archive";
        DeleteButton.SetState(HavenElementState.Disabled, space.IsBuiltIn);
        RenderFiles(space.Files);
        SetStatus(space.IsBuiltIn ? "Built-in Space · fork for an independent editable copy." : "Custom Space");
    }

    public void SetConversations(IReadOnlyList<SpaceConversation> conversations)
    {
        foreach (var child in ConversationsList.Children.ToArray()) ConversationsList.Remove(child);
        _conversationButtons.Clear();
        foreach (var conversation in conversations)
        {
            var button = Action($"Spaces.Conversation.{conversation.Id:N}", conversation.Title, ButtonVariant.Ghost);
            var id = conversation.Id;
            button.Invoked += (_, _) => ConversationSelected?.Invoke(this, id);
            ConversationsList.Add(button);
            _conversationButtons[id] = button;
        }
        if (conversations.Count == 0) ConversationsList.Add(new HuiText("No conversations in this Space yet.") { Level = TextLevel.Caption });
    }

    public SpaceEditorDraft ReadDraft()
    {
        var examples = string.IsNullOrWhiteSpace(ExampleUserInput.Text) && string.IsNullOrWhiteSpace(ExampleAssistantInput.Text)
            ? []
            : new[] { new SpaceExamplePair(ExampleUserInput.Text.Trim(), ExampleAssistantInput.Text.Trim()) };
        var surface = string.IsNullOrWhiteSpace(SurfaceTemplateInput.Text)
            ? null
            : new SpaceGeneratedSurface(SurfaceTemplateInput.Text.Trim(), string.IsNullOrWhiteSpace(SurfaceInputsInput.Text) ? "{}" : SurfaceInputsInput.Text.Trim());
        return new SpaceEditorDraft(NameInput.Text, DescriptionInput.Text, string.IsNullOrWhiteSpace(ModelInput.Text) ? null : ModelInput.Text.Trim(), InstructionsInput.Text, ThinkingMode, examples, surface);
    }

    public void SetStatus(string message) => StatusText.Content = message ?? string.Empty;

    private void RenderFiles(IReadOnlyList<SpaceFileReference> files)
    {
        foreach (var child in FilesList.Children.ToArray()) FilesList.Remove(child);
        foreach (var file in files)
        {
            var row = new Container { Layout = HavenLayout.Horizontal };
            row.SetValue(HavenProperties.Gap, HavenLength.Px(6));
            var label = new HuiText($"{file.DisplayName} · {(file.Permission == SpaceFilePermission.ReadWrite ? "read/write" : "read-only")}") { Level = TextLevel.Caption };
            label.SetValue(HavenProperties.Width, HavenLength.Fr(1));
            row.Add(label);
            var remove = Action($"Spaces.File.Remove.{file.DisplayName}", "Remove", ButtonVariant.Ghost);
            var path = file.Path;
            remove.Invoked += (_, _) => RemoveFileRequested?.Invoke(this, path);
            row.Add(remove);
            FilesList.Add(row);
        }
        if (files.Count == 0) FilesList.Add(new HuiText("No files attached.") { Level = TextLevel.Caption });
    }

    private void InvokeSelected(EventHandler<Guid>? handler)
    {
        if (_selectedSpaceId is { } id) handler?.Invoke(this, id);
    }

    private static Input NewInput(string name, string placeholder, bool multiline = false)
    {
        var input = new Input { Name = name, Placeholder = placeholder, Multiline = multiline };
        input.SetValue(HavenProperties.Width, HavenLength.Percent(100));
        if (multiline) input.SetValue(HavenProperties.MinHeight, HavenLength.Px(84));
        return input;
    }

    private static HuiButton Action(string name, string content, ButtonVariant variant)
    {
        var button = new HuiButton { Name = name, Content = content, Variant = variant };
        button.SetValue(HavenProperties.MinHeight, HavenLength.Px(38));
        return button;
    }
}
