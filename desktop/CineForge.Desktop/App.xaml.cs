using System.Windows;

namespace CineForge.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        LocalizationManager.Apply(LocalizationManager.DetectLanguage(), persist: false);
        MainWindow = new MainWindow();
        MainWindow.Show();
    }
}
