using GravityFalls.Shared;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;
using SavePuffle.Services;
using System.Collections.ObjectModel;

namespace GravityFalls.Client.Pages;

public partial class GamePage : ContentPage
{
    private const int Cols = 8;
    private const int Rows = 4;

    private const double CellSize = 72;
    private const double Gap = 8;
    private const double Pad = 12;
    private const double PawnSize = 28;

    private readonly Dictionary<int, Rect> _cellRects = new();
    private readonly Dictionary<int, Label> _pawnViews = new();
    private readonly ObservableCollection<PlayerCardVm> _playerCards = new();
    private readonly ObservableCollection<string> _events = new();

    private readonly Label _waddles = new()
    {
        Text = "🐷",
        FontSize = 22,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center,
        WidthRequest = PawnSize,
        HeightRequest = PawnSize,
        ZIndex = 30
    };

    private int _myId = -1;
    private GameStateDto? _state;

    private CancellationTokenSource? _diceAnimCts;

    public GamePage()
    {
        InitializeComponent();

        PlayersView.ItemsSource = _playerCards;
        EventsView.ItemsSource = _events;

        MeLabel.Text = $"Вы: {GameClient.Instance.Nickname}";

        RollButton.Clicked += async (_, __) => await OnRollClickedAsync();
        ExchangeButton.Clicked += async (_, __) => await OnExchangeAsync();
        LeaveButton.Clicked += async (_, __) => await OnLeaveAsync();

        BuildBoardOnce();

        if (GameClient.Instance.LastGameState != null)
            ApplyState(GameClient.Instance.LastGameState, animate: false);
        if (GameClient.Instance.LastDiceResult != null)
            SetDiceFace(GameClient.Instance.LastDiceResult.Value);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        GameClient.Instance.GameStateUpdated += OnGameState;
        GameClient.Instance.DiceResultReceived += OnDiceResult;
        GameClient.Instance.GameEventReceived += OnGameEvent;
        GameClient.Instance.GameOver += OnGameOver;
        GameClient.Instance.Error += OnError;
        GameClient.Instance.Disconnected += OnDisconnected;

        if (GameClient.Instance.LastGameState != null)
            ApplyState(GameClient.Instance.LastGameState, animate: false);
        if (GameClient.Instance.LastDiceResult != null)
            SetDiceFace(GameClient.Instance.LastDiceResult.Value);
    }

    protected override void OnDisappearing()
    {
        GameClient.Instance.GameStateUpdated -= OnGameState;
        GameClient.Instance.DiceResultReceived -= OnDiceResult;
        GameClient.Instance.GameEventReceived -= OnGameEvent;
        GameClient.Instance.GameOver -= OnGameOver;
        GameClient.Instance.Error -= OnError;
        GameClient.Instance.Disconnected -= OnDisconnected;
        base.OnDisappearing();
    }

    private void BuildBoardOnce()
    {
        BoardAbs.WidthRequest = Pad * 2 + Cols * CellSize + (Cols - 1) * Gap;
        BoardAbs.HeightRequest = Pad * 2 + Rows * CellSize + (Rows - 1) * Gap;

        BoardAbs.Children.Clear();
        _cellRects.Clear();

        for (int pos = 0; pos < Cols * Rows; pos++)
        {
            (int r, int c) = PosToRowCol(pos);
            double x = Pad + c * (CellSize + Gap);
            double y = Pad + r * (CellSize + Gap);

            var cell = CreateCell(pos);
            AbsoluteLayout.SetLayoutBounds(cell, new Rect(x, y, CellSize, CellSize));
            AbsoluteLayout.SetLayoutFlags(cell, AbsoluteLayoutFlags.None);
            BoardAbs.Children.Add(cell);

            _cellRects[pos] = new Rect(x, y, CellSize, CellSize);
        }

        AbsoluteLayout.SetLayoutBounds(_waddles, new Rect(0, 0, PawnSize, PawnSize));
        AbsoluteLayout.SetLayoutFlags(_waddles, AbsoluteLayoutFlags.None);
        _waddles.TranslationX = -9999;
        _waddles.TranslationY = -9999;
        BoardAbs.Children.Add(_waddles);
    }

    private View CreateCell(int pos)
    {
        if (pos > BoardConfig.FinishLine)
        {
            return new Border
            {
                Stroke = Color.FromArgb("#B8935E"),
                StrokeThickness = 2,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                BackgroundColor = Color.FromArgb("#F9EFD6"),
                Content = new Label
                {
                    Text = "—",
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = Color.FromArgb("#2B1B12"),
                    FontAttributes = FontAttributes.Bold
                }
            };
        }

        TileType tile = BoardConfig.GetTile(pos);
        string title = pos switch
        {
            0 => "START",
            30 => "FINISH",
            _ => pos.ToString()
        };

        (string icon, string sub) = TileToIcon(tile, pos);

        Color bg = tile switch
        {
            TileType.Start => Color.FromArgb("#E8C98A"),
            TileType.Finish => Color.FromArgb("#FFD6A6"),
            TileType.Mischief => Color.FromArgb("#F6D0D0"),
            TileType.Help => Color.FromArgb("#D9F4DA"),
            TileType.Exchange => Color.FromArgb("#E8C98A"),
            TileType.SkipTurn => Color.FromArgb("#F0E2FF"),
            TileType.ExtraTurn => Color.FromArgb("#D9E8FF"),
            TileType.DiscardHelp => Color.FromArgb("#FFECC7"),
            _ => Color.FromArgb("#F9EFD6")
        };

        var border = new Border
        {
            Stroke = Color.FromArgb("#B8935E"),
            StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            BackgroundColor = bg
        };

        var grid = new Grid { RowDefinitions = new RowDefinitionCollection { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) } };

        grid.Add(new Label
        {
            Text = title,
            FontSize = 11,
            TextColor = Color.FromArgb("#2B1B12"),
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(6, 4, 6, 0)
        });

        grid.Add(new Label
        {
            Text = icon,
            FontSize = 16,
            TextColor = Color.FromArgb("#2B1B12"),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        }, 0, 1);

        grid.Add(new Label
        {
            Text = sub,
            FontSize = 10,
            TextColor = Color.FromArgb("#7A5E3C"),
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4)
        }, 0, 2);

        border.Content = grid;
        return border;
    }

    private static (string icon, string sub) TileToIcon(TileType tile, int pos)
    {
        return tile switch
        {
            TileType.ArrowBlue => ("🔵➡️", BoardConfig.ArrowDeltaByPos.TryGetValue(pos, out var d1) ? $"+{d1}" : ""),
            TileType.ArrowRed => ("🔴⬅️", BoardConfig.ArrowDeltaByPos.TryGetValue(pos, out var d2) ? d2.ToString() : ""),
            TileType.Mischief => ("😈", "+1😈"),
            TileType.Help => ("✨", "+1✨"),
            TileType.Exchange => ("♻️", "обмен"),
            TileType.ExtraTurn => ("🔁", "ещё ход"),
            TileType.SkipTurn => ("💤", "пропуск"),
            TileType.DiscardHelp => ("🗑", "сброс"),
            TileType.Signpost => ("🪧", "указатель"),
            TileType.Totem => ("🗿", "тотем"),
            TileType.Finish => ("🏁", "победа"),
            TileType.Start => ("🚩", "старт"),
            _ => ("", "")
        };
    }

    private static (int row, int col) PosToRowCol(int pos)
    {
        int rowFromBottom = pos / Cols;
        int row = (Rows - 1) - rowFromBottom;

        int idxInRow = pos % Cols;
        bool reverse = (rowFromBottom % 2) == 1;
        int col = reverse ? (Cols - 1 - idxInRow) : idxInRow;

        return (row, col);
    }

    private async Task OnRollClickedAsync()
    {
        if (!GameClient.Instance.IsConnected)
        {
            await DisplayAlert("Network", "Нет соединения с сервером.", "OK");
            return;
        }

        if (_state == null)
        {
            await DisplayAlert("Game", "Ожидаем состояние игры от сервера...", "OK");
            return;
        }

        if (_myId < 0 || _state.CurrentTurnPlayerId != _myId)
        {
            await DisplayAlert("Ход", "Сейчас ход другого игрока.", "OK");
            return;
        }

        RollButton.IsEnabled = false;
        ExchangeButton.IsEnabled = false;
        HintLabel.Text = "Бросаем кубик...";

        StartDiceAnimation();

        try
        {
            await GameClient.Instance.RollDiceAsync();
        }
        catch (Exception ex)
        {
            StopDiceAnimation();
            await DisplayAlert("Network", ex.Message, "OK");
        }
    }

    private async Task OnExchangeAsync()
    {
        if (_state == null) return;
        if (_myId < 0 || _state.CurrentTurnPlayerId != _myId) return;

        try
        {
            await GameClient.Instance.ExchangeAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Network", ex.Message, "OK");
        }
    }

    private async Task OnLeaveAsync()
    {
        bool ok = await DisplayAlert("Выйти", "Отключиться от сервера и выйти в меню?", "Да", "Нет");
        if (!ok) return;

        GameClient.Instance.Disconnect();
        await Shell.Current.GoToAsync("//MainMenuPage");
    }

    private void OnGameState(GameStateDto state)
    {
        MainThread.BeginInvokeOnMainThread(() => ApplyState(state, animate: true));
    }

    private void OnDiceResult(DiceResultDto roll)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            StopDiceAnimation();
            SetDiceFace(roll.Value);

            try
            {
                await DiceBox.ScaleTo(1.15, 120, Easing.CubicOut);
                await DiceBox.ScaleTo(1.0, 140, Easing.CubicIn);
            }
            catch { }

            AddEvent($"🎲 {NameById(roll.PlayerId)} бросил: {roll.Value}");
        });
    }

    private void OnGameEvent(GameEventDto ev)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            string prefix = ev.Kind switch
            {
                GameEventKind.Good => "✅ ",
                GameEventKind.Bad => "⚠️ ",
                _ => "ℹ️ "
            };
            AddEvent(prefix + ev.Message);
        });
    }

    private void AddEvent(string text)
    {
        _events.Insert(0, text);
        while (_events.Count > 20) _events.RemoveAt(_events.Count - 1);
    }

    private void OnGameOver(GameOverDto over)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            StopDiceAnimation();
            RollButton.IsEnabled = false;
            ExchangeButton.IsEnabled = false;

            string msg = over.WinnerName == GameClient.Instance.Nickname
                ? "Ты спас Пухлю! 🎉"
                : $"Победил: {over.WinnerName}";

            await DisplayAlert("Игра окончена", msg, "OK");
        });
    }

    private void OnError(string message)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            StopDiceAnimation();
            await DisplayAlert("Error", message, "OK");
        });
    }

    private void OnDisconnected()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            StopDiceAnimation();
            RollButton.IsEnabled = false;
            ExchangeButton.IsEnabled = false;
            await DisplayAlert("Disconnected", "Соединение с сервером потеряно.", "OK");
            await Shell.Current.GoToAsync("//MainMenuPage");
        });
    }

    private void ApplyState(GameStateDto state, bool animate)
    {
        _state = state;

        if (_myId < 0)
        {
            var me = state.Players.FirstOrDefault(p => p.Name == GameClient.Instance.Nickname);
            if (me != null) _myId = me.Id;
        }

        TurnLabel.Text = $"Ходит: {NameById(state.CurrentTurnPlayerId)}";
        MeLabel.Text = _myId >= 0
            ? $"Вы: {GameClient.Instance.Nickname} (id={_myId})"
            : $"Вы: {GameClient.Instance.Nickname}";

        WaddlesInfo.Text = state.WaddlesPosition < 0
            ? "🐷 ещё не найден (ищи 🪧)"
            : (state.WaddlesCarrierId >= 0
                ? $"🐷 у {NameById(state.WaddlesCarrierId)}"
                : $"🐷 на клетке {state.WaddlesPosition}");

        // Player cards
        _playerCards.Clear();
        foreach (var p in state.Players.OrderBy(p => p.Id))
        {
            _playerCards.Add(new PlayerCardVm
            {
                Id = p.Id,
                Name = p.Name,
                Hero = p.Hero,
                Position = p.Position,
                HelpTokens = p.HelpTokens,
                MischiefTokens = p.MischiefTokens,
                HasWaddles = (state.WaddlesCarrierId == p.Id),
                IsTurn = (state.CurrentTurnPlayerId == p.Id),
                SkipNextTurn = p.SkipNextTurn,
                IsMe = (_myId == p.Id)
            });
        }

        // Buttons
        bool isMyTurn = (_myId >= 0 && state.CurrentTurnPlayerId == _myId);
        RollButton.IsEnabled = isMyTurn;

        ExchangeButton.IsEnabled = isMyTurn && CanExchange();
        HintLabel.Text = isMyTurn ? "Твой ход." : "Ожидание хода.";

        // Pawns
        foreach (var p in state.Players)
        {
            EnsurePawn(p.Id, p.Hero);
            MovePawn(p.Id, p.Position, animate);
        }

        // Waddles token
        if (state.WaddlesPosition >= 0)
        {
            MoveWaddles(state.WaddlesPosition, state.WaddlesCarrierId, animate);
        }
        else
        {
            _waddles.TranslationX = -9999;
            _waddles.TranslationY = -9999;
        }

        TryPulseCurrentTurn(state.CurrentTurnPlayerId);
    }

    private bool CanExchange()
    {
        if (_state == null || _myId < 0) return false;

        var me = _state.Players.FirstOrDefault(p => p.Id == _myId);
        if (me == null) return false;

        if (BoardConfig.GetTile(me.Position) != TileType.Exchange) return false;

        int cost = me.Hero == HeroType.Wendy ? 1 : 2;
        return me.MischiefTokens >= cost;
    }

    private void EnsurePawn(int playerId, HeroType hero)
    {
        if (_pawnViews.ContainsKey(playerId))
        {
            // Update emoji if needed
            _pawnViews[playerId].Text = HeroInfo.Emoji(hero);
            return;
        }

        var pawn = new Label
        {
            Text = HeroInfo.Emoji(hero),
            FontSize = 22,
            WidthRequest = PawnSize,
            HeightRequest = PawnSize,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            ZIndex = 20
        };

        AbsoluteLayout.SetLayoutBounds(pawn, new Rect(0, 0, PawnSize, PawnSize));
        AbsoluteLayout.SetLayoutFlags(pawn, AbsoluteLayoutFlags.None);
        pawn.TranslationX = -9999;
        pawn.TranslationY = -9999;

        _pawnViews[playerId] = pawn;
        BoardAbs.Children.Add(pawn);
    }

    private void MovePawn(int playerId, int position, bool animate)
    {
        if (!_pawnViews.TryGetValue(playerId, out var pawn)) return;
        if (!_cellRects.TryGetValue(position, out var cell)) return;

        var offset = OffsetForPlayer(playerId);

        double targetX = cell.X + (cell.Width / 2) - (PawnSize / 2) + offset.X;
        double targetY = cell.Y + (cell.Height / 2) - (PawnSize / 2) + offset.Y;

        if (!animate)
        {
            pawn.TranslationX = targetX;
            pawn.TranslationY = targetY;
            return;
        }

        _ = pawn.TranslateTo(targetX, targetY, 420, Easing.CubicInOut);
    }

    private void MoveWaddles(int position, int carrierId, bool animate)
    {
        if (!_cellRects.TryGetValue(position, out var cell)) return;

        var offset = carrierId >= 0 ? OffsetForPlayer(carrierId) : new Point(0, 0);

        double targetX = cell.X + (cell.Width / 2) - (PawnSize / 2) + offset.X;
        double targetY = cell.Y + (cell.Height / 2) - (PawnSize / 2) + offset.Y - 22;

        if (!animate)
        {
            _waddles.TranslationX = targetX;
            _waddles.TranslationY = targetY;
            return;
        }

        _ = AnimateWaddlesAsync(targetX, targetY);
    }

    private async System.Threading.Tasks.Task AnimateWaddlesAsync(double targetX, double targetY)
    {
        try
        {
            await _waddles.TranslateTo(targetX, targetY, 420, Easing.CubicInOut);
            await _waddles.ScaleTo(1.2, 120, Easing.CubicOut);
            await _waddles.ScaleTo(1.0, 140, Easing.CubicIn);
        }
        catch { }
    }

    private async void TryPulseCurrentTurn(int currentTurnId)
    {
        if (!_pawnViews.TryGetValue(currentTurnId, out var pawn)) return;

        try
        {
            await pawn.ScaleTo(1.25, 140, Easing.CubicOut);
            await pawn.ScaleTo(1.0, 160, Easing.CubicIn);
        }
        catch { }
    }

    private void SetDiceFace(int value)
    {
        DiceFace.Text = value <= 0 ? "?" : value.ToString();
        DiceBox.Rotation = 0;
    }

    private void StartDiceAnimation()
    {
        StopDiceAnimation();
        _diceAnimCts = new CancellationTokenSource();
        var cts = _diceAnimCts;

        DiceFace.Text = "…";
        DiceBox.Rotation = 0;

        _ = Task.Run(async () =>
        {
            try
            {
                while (cts != null && !cts.IsCancellationRequested)
                {
                    await MainThread.InvokeOnMainThreadAsync(() => DiceBox.RotateTo(DiceBox.Rotation + 360, 260, Easing.Linear));
                    await MainThread.InvokeOnMainThreadAsync(() => DiceBox.ScaleTo(1.08, 130, Easing.CubicOut));
                    await MainThread.InvokeOnMainThreadAsync(() => DiceBox.ScaleTo(1.0, 130, Easing.CubicIn));
                }
            }
            catch { }
        });
    }

    private void StopDiceAnimation()
    {
        try { _diceAnimCts?.Cancel(); } catch { }
        _diceAnimCts = null;
    }

    private string NameById(int id)
    {
        if (_state == null) return $"Player#{id}";
        return _state.Players.FirstOrDefault(p => p.Id == id)?.Name ?? $"Player#{id}";
    }

    private static Point OffsetForPlayer(int playerId) => (playerId % 4) switch
    {
        0 => new Point(-12, -10),
        1 => new Point(12, -10),
        2 => new Point(-12, 12),
        _ => new Point(12, 12)
    };

    public class PlayerCardVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public HeroType Hero { get; set; }
        public int Position { get; set; }
        public int HelpTokens { get; set; }
        public int MischiefTokens { get; set; }
        public bool HasWaddles { get; set; }
        public bool IsTurn { get; set; }
        public bool SkipNextTurn { get; set; }
        public bool IsMe { get; set; }

        public string HeroEmoji => HeroInfo.Emoji(Hero);
        public string NameLine => IsMe ? $"{Name} (ты)" : Name;
        public string PosChip => $"#{Position}";
        public string TokensChip => $"✨{HelpTokens} • 😈{MischiefTokens}";
        public bool SkipChipVisible => SkipNextTurn;
        public string TurnChip => IsTurn ? "➡️" : "";
    }
}
