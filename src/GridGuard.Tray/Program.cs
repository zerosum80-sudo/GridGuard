using System.IO.Pipes;

namespace GridGuard.Tray;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayContext());
    }
}

internal sealed class TrayContext : ApplicationContext
{
    private readonly NotifyIcon _icon;

    public TrayContext()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Status", null, async (_, _) => await SendAsync("status"));
        menu.Items.Add("Scan now (AuditOnly)", null, async (_, _) => await SendAsync("scan"));
        menu.Items.Add("Pause monitoring", null, async (_, _) => await SendAsync("pause"));
        menu.Items.Add("Resume monitoring", null, async (_, _) => await SendAsync("resume"));
        menu.Items.Add("Open logs", null, (_, _) => OpenFolder("logs"));
        menu.Items.Add("Open quarantine", null, (_, _) => OpenFolder("quarantine"));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit tray", null, (_, _) => ExitThread());
        _icon = new NotifyIcon
        {
            Text = "GridGuard - AuditOnly",
            Icon = SystemIcons.Shield,
            ContextMenuStrip = menu,
            Visible = true
        };
    }

    private async Task SendAsync(string command)
    {
        try
        {
            await using var pipe = new NamedPipeClientStream(
                ".", "GridGuard.Status.v1", PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(1500);
            using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(pipe, leaveOpen: true);
            await writer.WriteLineAsync(command);
            var response = await reader.ReadLineAsync() ?? "No status response.";
            _icon.ShowBalloonTip(3000, "GridGuard", response, ToolTipIcon.Info);
        }
        catch (Exception ex) when (ex is IOException or TimeoutException)
        {
            _icon.ShowBalloonTip(3000, "GridGuard unavailable", ex.Message, ToolTipIcon.Warning);
        }
    }

    private static void OpenFolder(string name)
    {
        var path = Path.Combine(AppContext.BaseDirectory, name);
        Directory.CreateDirectory(path);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    protected override void ExitThreadCore()
    {
        _icon.Visible = false;
        _icon.Dispose();
        base.ExitThreadCore();
    }
}

