using GravityFallsClient.Pages;

namespace GravityFallsClient;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // TODO: Роуты (навигация черцез Shell.Current.GoToAsync(nameof(...)))
        Routing.RegisterRoute(nameof(CharacterSelectionPage), typeof(CharacterSelectionPage));

        // На будущее,страницу игры:
        // Routing.RegisterRoute(nameof(GamePage), typeof(GamePage));
    }
}
