using GravityFalls.Server.Core;
using GravityFalls.Shared;

namespace GravityFalls.Server.Services
{
    public class GameLoopService
    {
        private readonly GameServer _server;
        private readonly MovementEngine _movement = new();
        private readonly Random _rng = new();
        private List<ClientSession> _players = new();
        private int _currentTurnIndex = 0;

        private int _waddlesPosition = -1;
        private int _waddlesCarrierId = -1;

        public bool IsGameStarted { get; private set; } = false;

        public GameLoopService(GameServer server)
        {
            _server = server;
        }

        public void StartGame(List<ClientSession> players)
        {
            IsGameStarted = true;
            _players = players;
            _currentTurnIndex = 0;
            _waddlesPosition = -1;
            _waddlesCarrierId = -1;

            foreach (var p in _players)
            {
                p.Position = 0;
                p.HasWaddles = false;
                p.HelpTokens = 0;
                p.MischiefTokens = 0;
                p.SkipNextTurn = false;
            }

            EmitEvent(GameEventKind.Info, "🎲 Игра началась! Выбирайте героев и спасайте Пухлю.");
            BroadcastState(lastDice: 0);
        }

        public void HandleDiceRoll(ClientSession player, RollDiceDto rollRequest)
        {
            if (!IsGameStarted) return;
            if (_players.Count == 0) return;

            if (_players[_currentTurnIndex].Id != player.Id) return;

            if (player.SkipNextTurn)
            {
                player.SkipNextTurn = false;
                EmitEvent(GameEventKind.Bad, $"💤 {player.Nickname} пропускает ход.");
                AdvanceTurn();
                BroadcastState(lastDice: 0);
                return;
            }

            if (rollRequest.TradeMischiefForHelp)
            {
                int cost = player.Hero == HeroType.Wendy ? 2 : 3;
                if (player.MischiefTokens >= cost)
                {
                    player.MischiefTokens -= cost;
                    player.HelpTokens += 1;
                    EmitEvent(GameEventKind.Good, player.Hero == HeroType.Wendy
                        ? "🪓 Венди: 2😈 → 1✨ (перед броском)"
                        : $"♻️ Обмен: {cost}😈 → 1✨ (перед броском)");
                }
                else
                {
                    EmitEvent(GameEventKind.Info, $"♻️ Недостаточно 😈 для обмена (нужно {cost}).");
                }
            }

            int roll = _rng.Next(1, 7);

            if (player.Hero == HeroType.Mabel && rollRequest.UseMabelBonus && IsWaddlesOnCellWithAnyPlayer())
            {
                roll += 1;
                EmitEvent(GameEventKind.Good, "🎀 Мэйбл: +1 к кубику (Пухля на клетке с игроком)");
            }

            _server.Broadcast(Packet.Serialize(OpCode.DiceResult, new DiceResultDto { PlayerId = player.Id, Value = roll }));

            var outcome = _movement.ExecuteTurn(player, roll, _players, ref _waddlesPosition, ref _waddlesCarrierId, dto =>
            {
                _server.Broadcast(Packet.Serialize(OpCode.GameEvent, dto));
            });

            if (player.Position >= BoardConfig.FinishLine && _waddlesCarrierId == player.Id)
            {
                var winPacket = Packet.Serialize(OpCode.GameOver, new GameOverDto { WinnerName = player.Nickname });
                _server.Broadcast(winPacket);
                IsGameStarted = false;
                return;
            }

            if (!outcome.ExtraTurn)
                AdvanceTurn();

            BroadcastState(lastDice: roll);
        }

        public void TryExchange(ClientSession player)
        {
            if (!IsGameStarted) return;
            if (_players.Count == 0) return;
            if (_players[_currentTurnIndex].Id != player.Id) return;

            bool changed = _movement.TryExchange(player, dto =>
            {
                _server.Broadcast(Packet.Serialize(OpCode.GameEvent, dto));
            });

            if (changed)
                BroadcastState(lastDice: 0);
        }

        private bool IsWaddlesOnCellWithAnyPlayer()
        {
            if (_waddlesPosition < 0) return false;
            return _players.Any(p => p.Position == _waddlesPosition);
        }

        private void AdvanceTurn()
        {
            if (_players.Count == 0) return;

            int safety = 0;
            do
            {
                _currentTurnIndex = (_currentTurnIndex + 1) % _players.Count;
                safety++;

                var p = _players[_currentTurnIndex];
                if (p.SkipNextTurn)
                {
                    p.SkipNextTurn = false;
                    EmitEvent(GameEventKind.Bad, $"💤 {p.Nickname} пропускает ход.");
                    continue;
                }

                break;

            } while (safety < 8);
        }

        private void BroadcastState(int lastDice)
        {
            var state = new GameStateDto
            {
                CurrentTurnPlayerId = _players[_currentTurnIndex].Id,
                WaddlesPosition = _waddlesPosition,
                WaddlesCarrierId = _waddlesCarrierId,
                LastDiceValue = lastDice
            };

            foreach (var p in _players.OrderBy(x => x.Id))
            {
                state.Players.Add(new PlayerStateDto
                {
                    Id = p.Id,
                    Name = p.Nickname,
                    Hero = p.Hero,
                    Position = p.Position,
                    HelpTokens = p.HelpTokens,
                    MischiefTokens = p.MischiefTokens,
                    SkipNextTurn = p.SkipNextTurn,
                    HasWaddles = (_waddlesCarrierId == p.Id)
                });
            }

            _server.Broadcast(Packet.Serialize(OpCode.GameState, state));
        }

        private void EmitEvent(GameEventKind kind, string msg)
        {
            _server.Broadcast(Packet.Serialize(OpCode.GameEvent, new GameEventDto { Kind = kind, Message = msg }));
        }
    }
}
