using GravityFalls.Client.Pages;

namespace GravityFalls.Client;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }
}
