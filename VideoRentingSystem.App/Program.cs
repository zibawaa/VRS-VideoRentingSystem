namespace VideoRentingSystem.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        // WinForms bootstrap: visual styles, default font behaviour, etc. for this TFM

        Application.Run(new MainForm());
        // blocks in a modal loop until MainForm closes, pumping window messages
    }
}
