using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CakeOS.HuiLinuxHost;
using CakeOS.Platform;
using CakeOS.Spaces.Hui;

namespace CakeOS.Spaces.LinuxHost;

/// <summary>
/// Linux graphical root provider for the real shared Spaces HUI.
/// Shell destinations remain behind ISpacesShellBridge so this provider does not fork product logic.
/// </summary>
public sealed class SpacesLinuxRootProvider : CakeOS.HuiLinuxHost.IHuiRootProvider, IDisposable
{
    private SpacesHuiController? _controller;
    private SpacesHuiRootElement? _root;
    private HuiRootLifecycleState _state = HuiRootLifecycleState.Uninitialized;
    private bool _disposed;

    public HuiRootProviderAbi Abi => HuiLinuxHostAbi.Current;

    public IRootElement CreateRoot(IServiceProvider services)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_root is not null) return _root;

        var registry = SpacesPlatformPaths.CreateDefaultRegistry();
        var conversationStore = SpacesPlatformPaths.CreateDefaultConversationStore();
        var conversations = new SpaceConversationService(conversationStore, registry);
        var application = new SpacesApplicationService(registry, conversations, new UnavailableCakeOsShellBridge());
        _controller = new SpacesHuiController(registry, conversations, application, new AvaloniaSpacesFilePicker());
        _root = new SpacesHuiRootElement(_controller.Scene.Root);
        return _root;
    }

    public async Task<HuiRootLifecycleState> InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _state = HuiRootLifecycleState.Loading;
        _ = CreateRoot(services);
        await _controller!.ActivateAsync(cancellationToken).ConfigureAwait(false);
        _state = HuiRootLifecycleState.Active;
        return _state;
    }

    public async Task ActivateAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_controller is null) throw new InvalidOperationException("Spaces root must be created before activation.");
        await _controller.ActivateAsync(cancellationToken).ConfigureAwait(false);
        _state = HuiRootLifecycleState.Active;
    }

    public Task DeactivateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _state = HuiRootLifecycleState.Suspended;
        return Task.CompletedTask;
    }

    public Task<HuiRootLifecycleState> GetStateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_state);
    }

    public Task ApplyThemeTokensAsync(HuiThemeTokens tokens, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task ApplyAccessibilityStateAsync(HuiAccessibilityState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        cancellationToken.ThrowIfCancellationRequested();
        _controller?.Scene.SetCompact(state.ScaleFactor >= 1.75);
        return Task.CompletedTask;
    }

    public Task<ProviderInjectionResult> InjectProvidersAsync(
        IReadOnlyCollection<ProviderDescriptor> providers,
        IReadOnlyCollection<ServiceDescriptor> services,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(services);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new ProviderInjectionResult(false, [], [], [], []));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _controller?.Dispose();
        _controller = null;
        _root = null;
        _state = HuiRootLifecycleState.Unavailable;
    }

    private sealed class AvaloniaSpacesFilePicker : ISpacesFilePicker
    {
        public async Task<IReadOnlyList<string>> PickFilesAsync(string title, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow is null)
                throw new InvalidOperationException("The Linux window is not available for file picking.");

            var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = string.IsNullOrWhiteSpace(title) ? "Add files to Space" : title,
                AllowMultiple = true
            });

            cancellationToken.ThrowIfCancellationRequested();
            return files.Select(file => file.TryGetLocalPath())
                .OfType<string>()
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToArray();
        }
    }

    private sealed class UnavailableCakeOsShellBridge : ISpacesShellBridge
    {
        public Task OpenHomeAsync(CancellationToken cancellationToken = default) =>
            Unavailable("CakeOS Home routing service is not present in this checkout.");

        public Task OpenUnscopedChatAsync(CancellationToken cancellationToken = default) =>
            Unavailable("CakeOS Chat routing service is not present in this checkout.");

        public Task OpenStudyAsync(SpaceDefinition space, SpaceLaunchPlan plan, CancellationToken cancellationToken = default) =>
            Unavailable("CakeOS Study routing service is not present in this checkout.");

        public Task OpenTasksAsync(SpaceDefinition space, SpaceLaunchPlan plan, CancellationToken cancellationToken = default) =>
            Unavailable("CakeOS Tasks routing service is not present in this checkout.");

        public Task OpenConfiguredChatAsync(
            SpaceDefinition space,
            SpaceLaunchPlan plan,
            SpaceConversation conversation,
            CancellationToken cancellationToken = default) =>
            Unavailable("CakeOS configured Chat routing service is not present in this checkout.");

        public Task OpenConversationAsync(SpaceConversation conversation, CancellationToken cancellationToken = default) =>
            Unavailable("CakeOS Chat conversation host is not present in this checkout.");

        public Task OpenSpaceLayoutAsync(SpaceDefinition space, CancellationToken cancellationToken = default) =>
            Unavailable("CakeOS graphical Space layout host is not present in this checkout.");

        private static Task Unavailable(string message) => Task.FromException(new InvalidOperationException(message));
    }
}
