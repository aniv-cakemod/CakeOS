using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CakeOS.Platform;
using Haven.UI.Components;

namespace CakeOS.HuiLinuxHost;

public sealed class PreviewWindow : Window
{
    private readonly bool _canvasMode = Environment.GetEnvironmentVariable("CAKEOS_HUI_CANVAS_PREVIEW") == "1";
    private readonly HuiPreviewSurface _surface;

    public PreviewWindow() : this((IRootElement?)null)
    {
    }

    public PreviewWindow(IRootElement? root)
    {
        _surface = root switch
        {
            null => new HuiPreviewSurface(),
            IHuiRootElement { NativeRoot: Haven.UI.Components.Page page } => new HuiPreviewSurface(page),
            _ => throw new NotSupportedException("The Linux HUI host can only render a compatible HUI Page root.")
        };

        Title = _canvasMode ? "CakeOS HUI Canvas / Rnote Preview" : "CakeOS HUI Linux Preview";
        Width = 960;
        Height = 600;
        MinWidth = 720;
        MinHeight = 480;
        Background = new SolidColorBrush(Color.Parse("#111318"));
        Content = _surface;
        Closed += (_, _) => _surface.Dispose();
    }

    public void RunInputSelfTest()
    {
        _surface.RunInputSelfTest();
        Title = _canvasMode
            ? "CakeOS HUI Canvas / Rnote Preview - input passed"
            : "CakeOS HUI Linux Preview - input passed";
    }
}
