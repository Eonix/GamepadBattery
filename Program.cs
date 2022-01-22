namespace GamepadBattery;

internal static class Program
{
    private static Mutex? _mutex;

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        // Ensure that only one instance of the application is running.
        _mutex = new Mutex(true, @"Global\GamepadBattery", out var isNewInstance);
        GC.KeepAlive(_mutex);
        if (!isNewInstance)
            return;

        ApplicationConfiguration.Initialize();
        Application.Run(new MainApplicationContext());
    }
}