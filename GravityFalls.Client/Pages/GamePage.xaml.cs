using GravityFalls.Shared;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;
using SavePuffle.Services;
using System.Collections.ObjectModel;
using SavePuffle.Models;
using System.Linq;

namespace GravityFalls.Client.Pages;

public partial class GamePage : ContentPage
{
    private const int Cols = 8;
    private const int Rows = 4;

    private const double CellSize = 70;
    private const double Gap = 6;
    private const double Pad = 12;
    private const double PawnSize = 28;

    private readonly Dictionary<int, Rect> _cellRects = new();
    private readonly Dictionary<int, Label> _pawnViews = new();
    private readonly Dictionary<int, HeroType> _heroByPlayer = new();
    private readonly ObservableCollection<string> _playerLines = new();

    private readonly Dictionary<int, TileType> _tileTypes = new()
    {
        [0] = TileType.Start,
        [2] = TileType.Help,
        [4] = TileType.Trap,
        [5] = TileType.ArrowBlue,
        [7] = TileType.Treasure,
        [9] = TileType.ArrowRed,
        [12] = TileType.Mischief,
        [14] = TileType.Help,
        [16] = TileType.Trap,
        [18] = TileType.ArrowBlue,
        [20] = TileType.Mischief,
        [22] = TileType.Treasure,
        [24] = TileType.ArrowRed,
        [26] = TileType.Help,
        [28] = TileType.Trap,
        [30] = TileType.Finish
    };

    private readonly Label _waddles = new()
    {
        Text = "🐷",
        FontSize = 22,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center,
        WidthRequest = PawnSize,
        HeightRequest = PawnSize,
        ZIndex = 20
    };

    private int _myId = -1;
    private GameStateDto? _state;

    private CancellationTokenSource? _diceAnimCts;

    public GamePage()
    {
        InitializeComponent();

        PlayersView.ItemsSource = _playerLines;

        MeLabel.Text = $"Вы: {GameClient.Instance.Nickname}";

        RollButton.Clicked += async (_, __) => await OnRollClickedAsync();
        LeaveButton.Clicked += async (_, __) => await OnLeaveAsync();

        BuildBoardOnce();

        // If we navigated after StartGame, we may already have state.
        if (GameClient.Instance.LastGameState != null)
            ApplyState(GameClient.Instance.LastGameState, animate: false);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        GameClient.Instance.GameStateUpdated += OnGameState;
        GameClient.Instance.DiceResultReceived += OnDiceResult;
        GameClient.Instance.GameOver += OnGameOver;
        GameClient.Instance.Error += OnError;
        GameClient.Instance.Disconnected += OnDisconnected;

        // Apply cached values (if any)
        if (GameClient.Instance.LastGameState != null)
            ApplyState(GameClient.Instance.LastGameState, animate: false);
        if (GameClient.Instance.LastDiceResult != null)
            SetDiceFace(GameClient.Instance.LastDiceResult.Value);
    }

    protected override void OnDisappearing()
    {
        GameClient.Instance.GameStateUpdated -= OnGameState;
        GameClient.Instance.DiceResultReceived -= OnDiceResult;
        GameClient.Instance.GameOver -= OnGameOver;
        GameClient.Instance.Error -= OnError;
        GameClient.Instance.Disconnected -= OnDisconnected;
        base.OnDisappearing();
    }

    private void BuildBoardOnce()
    {
        // Fixed-size board (scrollable), so pawn animation is predictable.
        BoardAbs.WidthRequest = Pad * 2 + Cols * CellSize + (Cols - 1) * Gap;
        BoardAbs.HeightRequest = Pad * 2 + Rows * CellSize + (Rows - 1) * Gap;

        BoardAbs.Children.Clear();
        _cellRects.Clear();

        // Build cells 0..31 (31 is just a filler, because 8x4=32).
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

        // Add pig token (moves between owners)
        AbsoluteLayout.SetLayoutBounds(_waddles, new Rect(0, 0, PawnSize, PawnSize));
        AbsoluteLayout.SetLayoutFlags(_waddles, AbsoluteLayoutFlags.None);
        _waddles.TranslationX = -9999;
        _waddles.TranslationY = -9999;
        BoardAbs.Children.Add(_waddles);

        // Add 4 pawn placeholders (created lazily when we know player IDs)
    }

    private View CreateCell(int pos)
    {
        var (title, icon, background) = DescribeCell(pos);

        var border = new Border
        {
            Stroke = Color.FromArgb("#B8935E"),
            StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            BackgroundColor = background
        };

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };

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
            FontSize = 14,
            TextColor = Color.FromArgb("#2B1B12"),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        }, 0, 1);

        border.Content = grid;
        return border;
    }

    private (string title, string icon, Color background) DescribeCell(int pos)
    {
        if (_tileTypes.TryGetValue(pos, out var type))
        {
            return type switch
            {
                TileType.Start => ("START", "🚩", Color.FromArgb("#E8C98A")),
                TileType.Finish => ("FINISH", "🏁", Color.FromArgb("#FFD6A6")),
                TileType.ArrowBlue => ("УСКОРЕНИЕ", "🔵➡️ +2", Color.FromArgb("#DEF1FF")),
                TileType.ArrowRed => ("ШИШКИ", "🔴⬅️ -2", Color.FromArgb("#FFE1E1")),
                TileType.Help => ("ПОДСКАЗКА", "🔦 +1/2", Color.FromArgb("#E8F5E9")),
                TileType.Mischief => ("ОЗОРСТВО", "😈 -2", Color.FromArgb("#FFF3CD")),
                TileType.Treasure => ("СУНДУК", "🪙 +1/2", Color.FromArgb("#FFF0D9")),
                TileType.Trap => ("ЛОВУШКА", "⚠️ -3", Color.FromArgb("#FBE9E7")),
                _ => (pos.ToString(), "", Color.FromArgb("#F9EFD6"))
            };
        }

        if (pos == 31) return ("—", "", Color.FromArgb("#F0F0F0"));
        return (pos.ToString(), "", Color.FromArgb("#F9EFD6"));
    }

    private static (int row, int col) PosToRowCol(int pos)
    {
        // Snake layout:
        // Row 3:  0..7   left->right
        // Row 2:  8..15  right->left
        // Row 1: 16..23  left->right
        // Row 0: 24..31  right->left
        int rowFromBottom = pos / Cols; // 0..3
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
        LogLabel.Text = "Отправили RollDice...";

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
        finally
        {
            // сервер пришлёт DiceResult + GameState, они уже включат/выключат кнопку
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

            // Animation #1 (dice): little bounce on result.
            try
            {
                await DiceBox.ScaleTo(1.15, 120, Easing.CubicOut);
                await DiceBox.ScaleTo(1.0, 140, Easing.CubicIn);
            }
            catch { /* ignore */ }

            LogLabel.Text = $"{NameById(roll.PlayerId)} бросил: {roll.Value}";
        });
    }

    private void OnGameOver(GameOverDto over)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            StopDiceAnimation();
            RollButton.IsEnabled = false;

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
            await DisplayAlert("Disconnected", "Соединение с сервером потеряно.", "OK");
            await Shell.Current.GoToAsync("//MainMenuPage");
        });
    }

    private void ApplyState(GameStateDto state, bool animate)
    {
        _state = state;

        // Resolve my ID once (server doesn't send explicit 'you are X', so match by nickname)
        if (_myId < 0)
        {
            var me = state.Players.FirstOrDefault(p => p.Name == GameClient.Instance.Nickname);
            if (me != null) _myId = me.Id;
        }

        // Sidebar list
        _playerLines.Clear();
        foreach (var p in state.Players.OrderBy(p => p.Id))
        {
            var heroInfo = HeroCatalog.ByType(p.Hero);
            _heroByPlayer[p.Id] = p.Hero;

            string turn = p.Id == state.CurrentTurnPlayerId ? "➡️" : "  ";
            string waddles = p.HasWaddles ? " 🐷" : "";
            _playerLines.Add($"{turn} {heroInfo.Emoji} {p.Name} • {heroInfo.Title} • клетка {p.Position}{waddles}");
        }

        // Header
        TurnLabel.Text = $"Ходит: {NameById(state.CurrentTurnPlayerId)}";
        if (_myId >= 0 && _heroByPlayer.TryGetValue(_myId, out var myHero))
        {
            var info = HeroCatalog.ByType(myHero);
            MeLabel.Text = $"Вы: {GameClient.Instance.Nickname} • {info.Emoji} {info.Title}";
        }
        else
        {
            MeLabel.Text = $"Вы: {GameClient.Instance.Nickname}";
        }

        // Roll button availability
        RollButton.IsEnabled = (_myId >= 0 && state.CurrentTurnPlayerId == _myId);

        // Pawns + movement
        foreach (var p in state.Players)
        {
            EnsurePawn(p.Id, p.Hero);
            MovePawn(p.Id, p.Position, animate);
        }

        // Pig token follows the owner (if any)
        var owner = state.Players.FirstOrDefault(p => p.HasWaddles);
        if (owner != null)
        {
            EnsurePawn(owner.Id, owner.Hero);
            MoveWaddles(owner.Id, owner.Position, animate);
        }
        else
        {
            _waddles.TranslationX = -9999;
            _waddles.TranslationY = -9999;
        }

        // Little turn highlight pulse (Animation #2 is pawn movement; this is extra but ok)
        TryPulseCurrentTurn(state.CurrentTurnPlayerId);
    }

    private void EnsurePawn(int playerId, HeroType hero)
    {
        if (_pawnViews.TryGetValue(playerId, out var existing))
        {
            existing.Text = PawnEmoji(hero);
            return;
        }

        var pawn = new Label
        {
            Text = PawnEmoji(hero),
            FontSize = 22,
            WidthRequest = PawnSize,
            HeightRequest = PawnSize,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            ZIndex = 10
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

        // Offset per player so they don't overlap in the same cell
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

    private void MoveWaddles(int ownerId, int position, bool animate)
    {
        if (!_cellRects.TryGetValue(position, out var cell)) return;

        // Place pig slightly above the owner's pawn.
        var ownerOffset = OffsetForPlayer(ownerId);

        double targetX = cell.X + (cell.Width / 2) - (PawnSize / 2) + ownerOffset.X;
        double targetY = cell.Y + (cell.Height / 2) - (PawnSize / 2) + ownerOffset.Y - 22;

        if (!animate)
        {
            _waddles.TranslationX = targetX;
            _waddles.TranslationY = targetY;
            return;
        }

        _ = MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                await _waddles.TranslateTo(targetX, targetY, 420, Easing.CubicInOut);
                await _waddles.ScaleTo(1.2, 120, Easing.CubicOut);
                await _waddles.ScaleTo(1.0, 140, Easing.CubicIn);
            }
            catch { /* ignore */ }
        });
    }

    private async void TryPulseCurrentTurn(int currentTurnId)
    {
        if (!_pawnViews.TryGetValue(currentTurnId, out var pawn)) return;

        try
        {
            await pawn.ScaleTo(1.25, 140, Easing.CubicOut);
            await pawn.ScaleTo(1.0, 160, Easing.CubicIn);
        }
        catch { /* ignore */ }
    }

    private void SetDiceFace(int value)
    {
        DiceFace.Text = value.ToString();
        DiceBox.Rotation = 0;
    }

    private void StartDiceAnimation()
    {
        StopDiceAnimation();
        _diceAnimCts = new CancellationTokenSource();

        DiceFace.Text = "…";
        DiceBox.Rotation = 0;

        _ = Task.Run(async () =>
        {
            try
            {
                while (!_diceAnimCts.IsCancellationRequested)
                {
                    await MainThread.InvokeOnMainThreadAsync(() => DiceBox.RotateTo(DiceBox.Rotation + 360, 260, Easing.Linear));
                    await MainThread.InvokeOnMainThreadAsync(() => DiceBox.ScaleTo(1.08, 130, Easing.CubicOut));
                    await MainThread.InvokeOnMainThreadAsync(() => DiceBox.ScaleTo(1.0, 130, Easing.CubicIn));
                }
            }
            catch { /* ignore */ }
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

    private static string PawnEmoji(HeroType hero) => hero switch
    {
        HeroType.Dipper => "🧢",
        HeroType.Mabel => "🎀",
        HeroType.Stan => "💼",
        HeroType.Soos => "🛠️",
        HeroType.Wendy => "🏹",
        _ => "👤"
    };

    private static Point OffsetForPlayer(int playerId) => (playerId % 4) switch
    {
        0 => new Point(-12, -10),
        1 => new Point(12, -10),
        2 => new Point(-12, 12),
        _ => new Point(12, 12)
    };
}
