// Entry point for the Windows desktop app. WinForms still expects Main() on a thread marked STAThread for COM/dialog interop.
namespace VideoRentingSystem.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Pulls in modern WinForms defaults (high DPI, etc.) without us manually setting every flag.
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
