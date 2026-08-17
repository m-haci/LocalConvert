using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinRT;

namespace LocalConvert.App;

public static class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(nint hWnd, string text, string caption, uint type);

    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            WriteStartupLog("Main started.");
            ComWrappersSupport.InitializeComWrappers();
            Application.Start(_ =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });
        }
        catch (Exception exception)
        {
            WriteStartupLog(exception.ToString());
            MessageBoxW(0, exception.ToString(), "LocalConvert", 0x00000010);
        }
    }

    internal static void ShowError(string title, Exception exception)
    {
        WriteStartupLog(exception.ToString());
        MessageBoxW(0, exception.ToString(), title, 0x00000010);
    }

    internal static void WriteStartupLog(string message)
    {
        try
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LocalConvert",
                "Logs");
            Directory.CreateDirectory(folder);
            File.AppendAllText(
                Path.Combine(folder, "startup.log"),
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch
        {
            // Logging must never prevent startup.
        }
    }
}
