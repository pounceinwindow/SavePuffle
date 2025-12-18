using GravityFalls.Shared;
using SavePuffle.Services;
using System.Collections.Generic;
using System.Linq;

namespace GravityFalls.Client.Pages;

public partial class MainMenuPage : ContentPage
{
    public MainMenuPage()
    {
        InitializeComponent();

        HeroPicker.ItemsSource = Enum.GetValues(typeof(HeroType)).Cast<HeroType>().ToList();
        HeroPicker.SelectedItem = HeroType.Dipper;
        HeroPicker.SelectedIndexChanged += (_, __) => UpdateHeroHint();
        UpdateHeroHint();

        StartButton.Clicked += StartButtonOnClicked;
        RulesButton.Clicked += async (_, __) =>
        {
            await DisplayAlert(
                "Rules",
                "1) Подключись к серверу\n2) В лобби нажми START, чтобы переключать готовность\n3) Когда все готовы — сервер стартанёт игру",
                "OK");
        };
        ExitButton.Clicked += (_, __) =>
        {
            try { Application.Current?.Quit(); } catch { /* platform-specific */ }
        };
    }

    private void UpdateHeroHint()
    {
        HeroType hero = HeroPicker.SelectedItem is HeroType picked ? picked : HeroType.Dipper;
        string hint = hero switch
        {
            HeroType.Dipper => "Диппер: +1 шаг если без Пухли, портал толкает сильнее.",
            HeroType.Mabel => "Мэйбл: обходит липкие ловушки и смягчает капканы.",
            HeroType.Stan => "Стэн: штрафы меньше — умеет выкручиваться.",
            HeroType.Soos => "Сус: снимает дебаффы и чинит технику прямо на поле.",
            HeroType.Wendy => "Вэнди: держит темп даже после ловушек.",
            _ => "У каждого героя своя фишка."
        };
        HeroHint.Text = hint;
    }

    private async void StartButtonOnClicked(object? sender, EventArgs e)
    {
        string nickname = (NicknameEntry.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(nickname))
        {
            await DisplayAlert("Nickname", "Введи никнейм.", "OK");
            return;
        }

        HeroType hero = HeroPicker.SelectedItem is HeroType picked ? picked : HeroType.Dipper;

        (string host, int port) = ParseHostPort((ServerEntry.Text ?? "").Trim());

        StartButton.IsEnabled = false;
        try
        {
            await GameClient.Instance.ConnectAsync(host, port, nickname, hero);
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

        // Accept: "ip", "ip:port", "host", "host:port"
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