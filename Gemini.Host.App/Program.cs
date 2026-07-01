namespace Gemini.Host.App;

internal class Program
{
    [STAThread()]
    public static void Main()
    {
        // Sets up High-DPI scaling, Visual Styles, and default fonts 
        // based on your application's project settings.
        ApplicationConfiguration.Initialize();

        // Starts the application message loop and opens your main form.
        Application.Run(new MainForm());
    }
}
