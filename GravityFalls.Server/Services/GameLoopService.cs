using GravityFalls.Server.Core;
using GravityFalls.Shared;

namespace GravityFalls.Server.Services
{
    public class GameLoopService
    {
        private GameServer _server;
        private MovementEngine _movement;
        private List<ClientSession> _players = new();
        private int _currentTurnIndex = 0;
        public bool IsGameStarted { get; private set; } = false;

        public GameLoopService(GameServer server)
        {
            _server = server;
            _movement = new MovementEngine();
        }

        public void StartGame(List<ClientSession> players)
        {
            IsGameStarted = true;
            _players = players;
            _currentTurnIndex = 0;
            foreach (var p in _players)
            {
                p.Position = 0;
                p.HasWaddles = false;
                p.NextStepLimit = null;
            }
            BroadcastState();
        }

        public void HandleDiceRoll(ClientSession player)
        {
            if (_players[_currentTurnIndex].Id != player.Id) return;

            Random rnd = new Random();
            int roll = rnd.Next(1, 7);

            var rollPacket = Packet.Serialize(OpCode.DiceResult, new DiceResultDto { PlayerId = player.Id, Value = roll });
            _server.Broadcast(rollPacket);

            _movement.ProcessMove(player, roll, _players);

            if (player.Position >= 30 && player.HasWaddles)
            {
                var winPacket = Packet.Serialize(OpCode.GameOver, new GameOverDto { WinnerName = player.Nickname });
                _server.Broadcast(winPacket);
                IsGameStarted = false;
            }
            else
            {
                _currentTurnIndex = (_currentTurnIndex + 1) % _players.Count;
                BroadcastState();
            }
        }

        private void BroadcastState()
        {
            var state = new GameStateDto();
            state.CurrentTurnPlayerId = _players[_currentTurnIndex].Id;
            state.WaddlesOwnerId = -1;

            foreach (var p in _players)
            {
                if (p.HasWaddles) state.WaddlesOwnerId = p.Id;

                state.Players.Add(new PlayerStateDto
                {
                    Id = p.Id,
                    Name = p.Nickname,
                    Position = p.Position,
                    HasWaddles = p.HasWaddles,
                    Hero = p.Hero,
                    Status = BuildStatus(p)
                });
            }

            _server.Broadcast(Packet.Serialize(OpCode.GameState, state));
        }

        private string BuildStatus(ClientSession player)
        {
            var parts = new List<string>();
            parts.Add(player.Hero switch
            {
                HeroType.Dipper => "📜 +1 шаг без Пухли",
                HeroType.Mabel => "🌈 Чарует ловушки",
                HeroType.Stan => "🪙 Меньше штрафов",
                HeroType.Soos => "🛠️ Снимает дебаффы",
                HeroType.Wendy => "🏹 Держит темп",
                _ => ""
            });

            if (player.HasWaddles) parts.Add("🐷 Пухля рядом");
            if (player.NextStepLimit.HasValue) parts.Add($"⏳ лимит {player.NextStepLimit}");

            return string.Join(" • ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }
    }
}