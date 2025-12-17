using GravityFalls.Shared;
using SavePuffle.Services;
using System.Collections.ObjectModel;

namespace GravityFalls.Client.Pages;

public partial class CharacterSelectionPage : ContentPage
{
    private readonly ObservableCollection<string> _slots = new();

    public CharacterSelectionPage()
    {
        InitializeComponent();

        CarouselView.ItemsSource = _slots;

        // Initial placeholder (until first LobbyUpdate arrives)
        for (int i = 0; i < 4; i++) _slots.Add("Ожидание игроков...");

        ConfirmButton.Clicked += async (_, __) =>
        {
            try { await GameClient.Instance.ToggleReadyAsync(); }
            catch (Exception ex) { await DisplayAlert("Network", ex.Message, "OK"); }
        };

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
                    _slots.Add($"{s.DisplayText} • {(s.IsReady ? "Готов" : "Не готов")}");
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
