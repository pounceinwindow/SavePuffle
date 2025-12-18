using GravityFalls.Server.Core;
using GravityFalls.Shared;

namespace GravityFalls.Server.Services
{
    public sealed class MovementEngine
    {
        private readonly Random _rng = new();

        public TurnOutcome ExecuteTurn(ClientSession player, int diceValue, List<ClientSession> allPlayers,
            ref int waddlesPosition, ref int waddlesCarrierId, Action<GameEventDto> emit)
        {
            bool extraTurn = false;

            // Carry state at start
            bool carrying = (waddlesCarrierId == player.Id);
            int stepsWithWaddles = 0;

            // --- Forward movement (dice) ---
            int max = BoardConfig.FinishLine;
            for (int i = 0; i < diceValue; i++)
            {
                if (carrying && stepsWithWaddles >= 3)
                {
                    emit(new GameEventDto { Kind = GameEventKind.Info, Message = "🐷 С Пухлей можно пройти максимум 3 клетки за ход." });
                    break;
                }

                int next = Math.Min(player.Position + 1, max);
                if (next == player.Position) break;

                player.Position = next;

                if (carrying)
                {
                    stepsWithWaddles++;
                    waddlesPosition = player.Position;
                }

                // Auto-pickup / steal when passing the Waddles cell
                if (waddlesPosition >= 0 && player.Position == waddlesPosition && waddlesCarrierId != player.Id)
                {
                    // Drop from previous carrier
                    if (waddlesCarrierId >= 0)
                    {
                        int carrierIdSnapshot = waddlesCarrierId;
                        var prev = allPlayers.FirstOrDefault(p => p.Id == carrierIdSnapshot);
                        if (prev != null) prev.HasWaddles = false;
                    }

                    waddlesCarrierId = player.Id;
                    player.HasWaddles = true;

                    // IMPORTANT (rulebook): steps before pickup don't matter => you still can do up to 3 cells AFTER pickup.
                    carrying = true;
                    stepsWithWaddles = 0;

                    // Pig follows the carrier
                    waddlesPosition = player.Position;

                    emit(new GameEventDto { Kind = GameEventKind.Good, Message = $"🐷 {player.Nickname} подхватил(а) Пухлю!" });
                }
            }

            // If we are carrying, keep pig at our final cell
            if (waddlesCarrierId == player.Id)
            {
                player.HasWaddles = true;
                waddlesPosition = player.Position;
            }

            // --- Special cell (ONLY if arrived moving forward) ---
            TileType tile = BoardConfig.GetTile(player.Position);
            switch (tile)
            {
                case TileType.ArrowBlue:
                case TileType.ArrowRed:
                    {
                        if (tile == TileType.ArrowRed && player.Hero == HeroType.Soos)
                        {
                            emit(new GameEventDto { Kind = GameEventKind.Good, Message = $"😎 Зус игнорирует красные стрелки." });
                            break;
                        }

                        if (BoardConfig.ArrowDeltaByPos.TryGetValue(player.Position, out int delta))
                        {
                            int before = player.Position;

                            // If we are forced back while carrying - drop Waddles (rulebook).
                            if (delta < 0)
                                DropWaddlesIfCarrying(player, ref waddlesPosition, ref waddlesCarrierId, emit);

                            player.Position = Clamp(player.Position + delta);

                            emit(new GameEventDto
                            {
                                Kind = delta > 0 ? GameEventKind.Good : GameEventKind.Bad,
                                Message = $"➡️ Стрелка: {before} → {player.Position}"
                            });
                        }
                        break;
                    }

                case TileType.Help:
                    {
                        int before = player.HelpTokens;
                        if (player.Hero == HeroType.Dipper && before == 0)
                        {
                            player.HelpTokens += 2; // 1 for the cell + 1 bonus
                            emit(new GameEventDto { Kind = GameEventKind.Good, Message = $"🧢 Диппер: ✨ было 0, получаешь +2✨" });
                        }
                        else
                        {
                            player.HelpTokens += 1;
                            emit(new GameEventDto { Kind = GameEventKind.Good, Message = $"✨ {player.Nickname} получает +1✨" });
                        }
                        break;
                    }

                case TileType.Mischief:
                    {
                        bool stanNoEffect = (player.Hero == HeroType.Stan && player.MischiefTokens == 0);
                        player.MischiefTokens += 1;

                        if (stanNoEffect)
                        {
                            emit(new GameEventDto { Kind = GameEventKind.Info, Message = $"💼 Стэн: первая пакость без эффекта." });
                            break;
                        }

                        ApplyRandomMischief(player, allPlayers, ref waddlesPosition, ref waddlesCarrierId, emit);
                        break;
                    }

                case TileType.Exchange:
                    emit(new GameEventDto { Kind = GameEventKind.Info, Message = "♻️ Клетка обмена: можно обменять 😈 на ✨ (кнопка в UI)." });
                    break;

                case TileType.ExtraTurn:
                    extraTurn = true;
                    emit(new GameEventDto { Kind = GameEventKind.Good, Message = $"🔁 {player.Nickname} получает ещё ход!" });
                    break;

                case TileType.SkipTurn:
                    if (!player.SkipNextTurn)
                    {
                        player.SkipNextTurn = true;
                        emit(new GameEventDto { Kind = GameEventKind.Bad, Message = $"💤 {player.Nickname} пропустит следующий ход." });
                    }
                    else
                    {
                        emit(new GameEventDto { Kind = GameEventKind.Info, Message = "💤 Нельзя пропустить два хода подряд." });
                    }
                    break;

                case TileType.DiscardHelp:
                    player.HelpTokens = 0;
                    emit(new GameEventDto { Kind = GameEventKind.Bad, Message = $"🗑 {player.Nickname} сбрасывает все ✨" });
                    break;

                case TileType.Signpost:
                    {
                        if (waddlesPosition < 0)
                        {
                            int spawn = _rng.Next(1, BoardConfig.FinishLine); // 1..29
                            waddlesPosition = spawn;
                            waddlesCarrierId = -1;
                            foreach (var p in allPlayers) p.HasWaddles = false;

                            emit(new GameEventDto { Kind = GameEventKind.Info, Message = $"🪧 Указатель: Пухля появился на клетке {spawn}." });
                        }
                        else
                        {
                            emit(new GameEventDto { Kind = GameEventKind.Info, Message = "🪧 Указатель: Пухля уже на поле." });
                        }
                        break;
                    }

                case TileType.Totem:
                    {
                        // In the full boardgame this opens a Wonder Shack card.
                        // For this project variant A we give a small bonus.
                        player.HelpTokens += 1;
                        emit(new GameEventDto { Kind = GameEventKind.Good, Message = $"🗿 Тотем: {player.Nickname} получает +1✨" });
                        break;
                    }
            }

            // Sync HasWaddles flag for everyone
            foreach (var p in allPlayers)
                p.HasWaddles = (waddlesCarrierId == p.Id);

            return new TurnOutcome { ExtraTurn = extraTurn };
        }

        public bool TryExchange(ClientSession player, Action<GameEventDto> emit)
        {
            if (BoardConfig.GetTile(player.Position) != TileType.Exchange)
            {
                emit(new GameEventDto { Kind = GameEventKind.Info, Message = "♻️ Обмен доступен только на клетке обмена." });
                return false;
            }

            int cost = player.Hero == HeroType.Wendy ? 1 : 2;
            if (player.MischiefTokens < cost)
            {
                emit(new GameEventDto { Kind = GameEventKind.Info, Message = $"♻️ Недостаточно 😈 для обмена (нужно {cost})." });
                return false;
            }

            player.MischiefTokens -= cost;
            player.HelpTokens += 1;

            emit(new GameEventDto
            {
                Kind = GameEventKind.Good,
                Message = player.Hero == HeroType.Wendy
                    ? "🪓 Венди: 1😈 → 1✨"
                    : $"♻️ Обмен: {cost}😈 → 1✨"
            });

            return true;
        }

        private void ApplyRandomMischief(ClientSession player, List<ClientSession> allPlayers,
            ref int waddlesPosition, ref int waddlesCarrierId, Action<GameEventDto> emit)
        {
            // A small pool of debuffs based on the rulebook examples.
            int roll = _rng.Next(0, 4);
            switch (roll)
            {
                case 0:
                    Back(player, 2, ref waddlesPosition, ref waddlesCarrierId, emit);
                    emit(new GameEventDto { Kind = GameEventKind.Bad, Message = "😈 Пакость: вернись на 2 клетки назад." });
                    break;

                case 1:
                    {
                        int d = _rng.Next(1, 7);
                        Back(player, d, ref waddlesPosition, ref waddlesCarrierId, emit);
                        emit(new GameEventDto { Kind = GameEventKind.Bad, Message = $"😈 Пакость: брось кубик и вернись на {d}." });
                        break;
                    }

                case 2:
                    if (!player.SkipNextTurn)
                    {
                        player.SkipNextTurn = true;
                        emit(new GameEventDto { Kind = GameEventKind.Bad, Message = "😈 Пакость: пропусти следующий ход." });
                    }
                    else
                    {
                        emit(new GameEventDto { Kind = GameEventKind.Info, Message = "😈 Пакость: пропуск уже активен (не суммируется)." });
                    }
                    break;

                default:
                    if (player.HelpTokens > 0)
                    {
                        player.HelpTokens = Math.Max(0, player.HelpTokens - 1);
                        emit(new GameEventDto { Kind = GameEventKind.Bad, Message = "😈 Пакость: сбрось 1✨." });
                    }
                    else
                    {
                        emit(new GameEventDto { Kind = GameEventKind.Info, Message = "😈 Пакость: у тебя нет ✨." });
                    }
                    break;
            }
        }

        private void Back(ClientSession player, int steps, ref int waddlesPosition, ref int waddlesCarrierId, Action<GameEventDto> emit)
        {
            if (steps <= 0) return;
            DropWaddlesIfCarrying(player, ref waddlesPosition, ref waddlesCarrierId, emit);

            player.Position = Clamp(player.Position - steps);
        }

        private void DropWaddlesIfCarrying(ClientSession player, ref int waddlesPosition, ref int waddlesCarrierId, Action<GameEventDto> emit)
        {
            if (waddlesCarrierId != player.Id) return;

            // Rulebook: Waddles never moves back. So we drop him where we were BEFORE moving back.
            waddlesPosition = player.Position;
            waddlesCarrierId = -1;
            player.HasWaddles = false;

            emit(new GameEventDto { Kind = GameEventKind.Info, Message = "🐷 Пухля не ходит назад — остался на клетке." });
        }

        private static int Clamp(int pos)
        {
            if (pos < 0) return 0;
            if (pos > BoardConfig.FinishLine) return BoardConfig.FinishLine;
            return pos;
        }
    }

    public sealed class TurnOutcome
    {
        public bool ExtraTurn { get; set; }
    }
}
