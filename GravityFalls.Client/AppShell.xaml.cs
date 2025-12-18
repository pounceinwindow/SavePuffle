using GravityFalls.Client.Pages;

namespace GravityFalls.Client;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // TODO: Роуты (навигация черцез Shell.Current.GoToAsync(nameof(...)))
        Routing.RegisterRoute(nameof(CharacterSelectionPage), typeof(CharacterSelectionPage));

        // Страница игры
        Routing.RegisterRoute(nameof(GamePage), typeof(GamePage));
    }
}
