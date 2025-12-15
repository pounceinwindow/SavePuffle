namespace GravityFallsClient.Pages;

public partial class MainMenuPage : ContentPage
{
    public MainMenuPage()
    {
        InitializeComponent();
        StartButton.Clicked += async (_, __) =>
        {
            await Shell.Current.GoToAsync(nameof(CharacterSelectionPage));
        };
    }


}
//TODO: StartButton.Clicked 
//возьмёт значения из Entry(может по визуальному дереву или x:Name)
//подключится к серверу
//перейдёт на CharacterSelectionPage
//RulesButton → покажет правила
//ExitButton → закроет приложение