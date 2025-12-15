using GravityFallsClient;

namespace SavePuffle;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }
}
