using SavePuffle.Services;

namespace GravityFalls.Client.Pages;

public partial class MainMenuPage : ContentPage
{
    public MainMenuPage()
    {
        InitializeComponent();

        StartButton.Clicked += StartButtonOnClicked;
        RulesButton.Clicked += async (_, __) =>
        {
            var closeBtn = new Button
            {
                Text = "Закрыть",
                VerticalOptions = LayoutOptions.End
            };

            var page = new ContentPage
            {
                Content = new Grid
                {
                    Padding = 20,
                    Children =
            {
                new Image { Source = "rules.png", Aspect = Aspect.AspectFit },
                closeBtn
            }
                }
            };

            closeBtn.Clicked += async (_, __) => await page.Navigation.PopModalAsync();
            await Navigation.PushModalAsync(page);
        };

        ExitButton.Clicked += (_, __) =>
        {
            try { Application.Current?.Quit(); } catch { }
        };
    }

    private async void StartButtonOnClicked(object? sender, EventArgs e)
    {
        string nickname = (NicknameEntry.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(nickname))
        {
            await DisplayAlert("Nickname", "Введи никнейм.", "OK");
            return;
        }

        (string host, int port) = ParseHostPort((ServerEntry.Text ?? "").Trim());

        StartButton.IsEnabled = false;
        try
        {
            await GameClient.Instance.ConnectAsync(host, port, nickname);
            await Shell.Current.GoToAsync(nameof(CharacterSelectionPage));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Connect failed", ex.Message, "OK");
            GameClient.Instance.Disconnect();
        }
        finally
        {
            StartButton.IsEnabled = true;
        }
    }

    private static (string host, int port) ParseHostPort(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return ("127.0.0.1", 8888);

        string host = input;
        int port = 8888;

        int idx = input.LastIndexOf(':');
        if (idx > 0 && idx < input.Length - 1 && int.TryParse(input[(idx + 1)..], out int parsedPort))
        {
            host = input[..idx];
            port = parsedPort;
        }

        return (host, port);
    }
}