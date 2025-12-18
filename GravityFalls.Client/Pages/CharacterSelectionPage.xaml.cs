using GravityFalls.Shared;
using SavePuffle.Services;
using System.Collections.ObjectModel;
using SavePuffle.Models;
using System.Linq;

namespace GravityFalls.Client.Pages;

public partial class CharacterSelectionPage : ContentPage
{
    private readonly ObservableCollection<string> _slots = new();
    private HeroType _currentHero = HeroType.Dipper;

    public CharacterSelectionPage()
    {
        InitializeComponent();

        CarouselView.ItemsSource = _slots;
        HeroPicker.ItemsSource = HeroCatalog.All;

        var firstHero = HeroCatalog.ByType(_currentHero);
        HeroPicker.SelectedItem = firstHero;
        HeroDescriptionLabel.Text = FormatHero(firstHero);

        // Initial placeholder (until first LobbyUpdate arrives)
        for (int i = 0; i < 4; i++) _slots.Add("Ожидание игроков...");

        ConfirmButton.Clicked += async (_, __) =>
        {
            try { await GameClient.Instance.ToggleReadyAsync(); }
            catch (Exception ex) { await DisplayAlert("Network", ex.Message, "OK"); }
        };

        HeroPicker.SelectionChanged += async (_, e) => await OnHeroSelectedAsync(e);

        BackButton.Clicked += async (_, __) =>
        {
            GameClient.Instance.Disconnect();
            await Shell.Current.GoToAsync("..");
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        GameClient.Instance.LobbyUpdated += OnLobbyUpdated;
        GameClient.Instance.GameStarted += OnGameStarted;
        GameClient.Instance.Error += OnError;
        GameClient.Instance.Disconnected += OnDisconnected;
    }

    protected override void OnDisappearing()
    {
        GameClient.Instance.LobbyUpdated -= OnLobbyUpdated;
        GameClient.Instance.GameStarted -= OnGameStarted;
        GameClient.Instance.Error -= OnError;
        GameClient.Instance.Disconnected -= OnDisconnected;
        base.OnDisappearing();
    }

    private async Task OnHeroSelectedAsync(SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not HeroInfo hero) return;
        if (hero.Type == _currentHero) return;

        _currentHero = hero.Type;
        HeroDescriptionLabel.Text = FormatHero(hero);

        try
        {
            await GameClient.Instance.SetHeroAsync(hero.Type);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Network", ex.Message, "OK");
        }
    }

    private static string FormatHero(HeroInfo hero) => $"{hero.Emoji} {hero.Title}: {hero.Passive}";

    private void OnLobbyUpdated(LobbyStateDto lobby)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _slots.Clear();
            foreach (var s in lobby.Slots.OrderBy(x => x.SlotIndex))
            {
                if (s.IsEmpty)
                {
                    _slots.Add("Свободный слот • Подключиться");
                }
                else
                {
                    var hero = HeroCatalog.ByType(s.Hero);
                    _slots.Add($"{hero.Emoji} {s.DisplayText} • {(s.IsReady ? "Готов" : "Не готов")}");
                    if (s.DisplayText == GameClient.Instance.Nickname && s.Hero != _currentHero)
                    {
                        _currentHero = s.Hero;
                        var info = HeroCatalog.ByType(s.Hero);
                        HeroPicker.SelectedItem = info;
                        HeroDescriptionLabel.Text = FormatHero(info);
                    }
                }
            }
        });
    }

    private void OnGameStarted()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await DisplayAlert("Start", "Все готовы — игра стартует!", "OK");
            await Shell.Current.GoToAsync(nameof(GamePage));
        });
    }

    private void OnError(string message)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await DisplayAlert("Error", message, "OK");
        });
    }

    private void OnDisconnected()
    {
        // If user explicitly clicked 'Back', we're already navigating away.
        // Otherwise: show quick hint.
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_slots.Count == 0)
                _slots.Add("Disconnected");
        });
    }
}
