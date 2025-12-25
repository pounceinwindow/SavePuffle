using GravityFalls.Client.Pages;

namespace GravityFalls.Client;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(CharacterSelectionPage), typeof(CharacterSelectionPage));

        Routing.RegisterRoute(nameof(GamePage), typeof(GamePage));
    }
}
