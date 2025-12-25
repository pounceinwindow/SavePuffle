using GravityFalls.Shared;
using SavePuffle.Services;
using System.Collections.ObjectModel;

namespace GravityFalls.Client.Pages;

public partial class CharacterSelectionPage : ContentPage
{
    private readonly ObservableCollection<LobbySlotVm> _slots = new();

    private int _myId = -1;
    private HeroType _selectedHero = HeroType.Dipper;

    public CharacterSelectionPage()
    {
        InitializeComponent();

        CarouselView.ItemsSource = _slots;

        HeroDipper.Clicked += async (_, __) => await PickHeroAsync(HeroType.Dipper);
        HeroMabel.Clicked += async (_, __) => await PickHeroAsync(HeroType.Mabel);
        HeroStan.Clicked += async (_, __) => await PickHeroAsync(HeroType.Stan);
        HeroSoos.Clicked += async (_, __) => await PickHeroAsync(HeroType.Soos);
        HeroWendy.Clicked += async (_, __) => await PickHeroAsync(HeroType.Wendy);

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

        UpdateHeroHint();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        GameClient.Instance.LobbyUpdated += OnLobbyUpdated;
        GameClient.Instance.GameStarted += OnGameStarted;
        GameClient.Instance.Error += OnError;
        GameClient.Instance.Disconnected += OnDisconnected;

        if (GameClient.Instance.LastLobbyState != null)
            OnLobbyUpdated(GameClient.Instance.LastLobbyState);
    }

    protected override void OnDisappearing()
    {
        GameClient.Instance.LobbyUpdated -= OnLobbyUpdated;
        GameClient.Instance.GameStarted -= OnGameStarted;
        GameClient.Instance.Error -= OnError;
        GameClient.Instance.Disconnected -= OnDisconnected;
        base.OnDisappearing();
    }

    private async Task PickHeroAsync(HeroType hero)
    {
        _selectedHero = hero;
        UpdateHeroHint();

        if (!GameClient.Instance.IsConnected) return;

        try
        {
            await GameClient.Instance.SelectHeroAsync(hero);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Network", ex.Message, "OK");
        }
    }

    private void UpdateHeroHint()
    {
        HeroHint.Text = $"Выбран: {HeroInfo.Emoji(_selectedHero)} {HeroInfo.DisplayName(_selectedHero)} • {HeroInfo.AbilitySummary(_selectedHero)}";
    }

    private void OnLobbyUpdated(LobbyStateDto lobby)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _slots.Clear();

            var takenHeroes = new HashSet<HeroType>();

            foreach (var s in lobby.Slots.OrderBy(x => x.SlotIndex))
            {
                if (!s.IsEmpty)
                {
                    takenHeroes.Add(s.Hero);
                    if (s.Nickname == GameClient.Instance.Nickname) _myId = s.PlayerId;
                }

                _slots.Add(new LobbySlotVm
                {
                    SlotIndex = s.SlotIndex,
                    PlayerId = s.PlayerId,
                    Nickname = s.Nickname,
                    Hero = s.Hero,
                    IsReady = s.IsReady,
                    IsEmpty = s.IsEmpty,
                    IsMe = (!s.IsEmpty && s.Nickname == GameClient.Instance.Nickname)
                });
            }

            SetHeroButtonState(HeroDipper, HeroType.Dipper, takenHeroes);
            SetHeroButtonState(HeroMabel, HeroType.Mabel, takenHeroes);
            SetHeroButtonState(HeroStan, HeroType.Stan, takenHeroes);
            SetHeroButtonState(HeroSoos, HeroType.Soos, takenHeroes);
            SetHeroButtonState(HeroWendy, HeroType.Wendy, takenHeroes);

            var me = lobby.Slots.FirstOrDefault(x => x.PlayerId == _myId);
            if (me != null && !me.IsEmpty)
                ConfirmButton.Text = me.IsReady ? "UNREADY" : "READY";

        });
    }

    private void SetHeroButtonState(Button btn, HeroType hero, HashSet<HeroType> taken)
    {
        bool takenByOther = taken.Contains(hero);
        if (_myId >= 0)
        {
            var mine = _slots.FirstOrDefault(s => s.PlayerId == _myId);
            if (mine != null && mine.Hero == hero) takenByOther = false;
        }

        btn.IsEnabled = !takenByOther;
        btn.Opacity = btn.IsEnabled ? 1 : 0.45;

        btn.BackgroundColor = hero == _selectedHero ? Color.FromArgb("#E8C98A") : Color.FromArgb("#F9EFD6");
    }

    private void OnGameStarted()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
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
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_slots.Count == 0)
                _slots.Add(item: new LobbySlotVm { IsEmpty = true, Nickname = "Disconnected" });
        });
    }

    public class LobbySlotVm
    {
        public int SlotIndex { get; set; }
        public int PlayerId { get; set; }
        public string Nickname { get; set; } = "";
        public HeroType Hero { get; set; } = HeroType.Dipper;
        public bool IsReady { get; set; }
        public bool IsEmpty { get; set; }
        public bool IsMe { get; set; }

        public string HeroEmoji => IsEmpty ? "➕" : HeroInfo.Emoji(Hero);
        public string Title => IsEmpty ? $"Слот {SlotIndex + 1}: свободен" : $"{Nickname} — {HeroInfo.DisplayName(Hero)}";
        public string ReadyChip => IsEmpty ? "" : (IsReady ? "✅" : "⏳");
        public string StatusText => IsEmpty ? "Ждём игрока" : (IsReady ? "Готов" : "Не готов");
        public Color StatusDotColor => IsEmpty ? Color.FromArgb("#7A5E3C") : (IsReady ? Color.FromArgb("#2E7D32") : Color.FromArgb("#B71C1C"));
        public string MeTag => IsMe ? "(это ты)" : "";
    }
}
