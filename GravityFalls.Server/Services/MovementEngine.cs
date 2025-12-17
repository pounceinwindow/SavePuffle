using GravityFalls.Server.Core;
using GravityFalls.Shared;

namespace GravityFalls.Server.Services
{
    public class MovementEngine
    {
        private List<TileType> _map;
        private const int FINISH_LINE = 30;

        public MovementEngine()
        {
            GenerateMap();
        }

        private void GenerateMap()
        {
            _map = new List<TileType>();
            for (int i = 0; i <= FINISH_LINE; i++) _map.Add(TileType.Empty);

            _map[5] = TileType.ArrowBlue;
            _map[12] = TileType.ArrowRed;
            _map[20] = TileType.Mischief;
            _map[30] = TileType.Finish;
        }

        public void ProcessMove(ClientSession player, int diceRoll, List<ClientSession> allPlayers)
        {
            int actualSteps = diceRoll;
            if (player.HasWaddles)
            {
                actualSteps = Math.Min(diceRoll, 3);
            }

            int startPos = player.Position;
            int endPos = startPos + actualSteps;

            var waddlesOwner = allPlayers.FirstOrDefault(p => p.HasWaddles && p.Id != player.Id);
            if (waddlesOwner != null)
            {
                if (endPos >= waddlesOwner.Position)
                {
                    Console.WriteLine($"{player.Nickname} stole Waddles from {waddlesOwner.Nickname}!");
                    waddlesOwner.HasWaddles = false;
                    player.HasWaddles = true;
                }
            }
            else if (allPlayers.All(p => !p.HasWaddles) && endPos >= 10)
            {
                player.HasWaddles = true;
            }

            player.Position = Math.Min(endPos, FINISH_LINE);

            if (_map[player.Position] == TileType.ArrowBlue) player.Position += 2;
            if (_map[player.Position] == TileType.ArrowRed) player.Position -= 2;

            if (player.Position < 0) player.Position = 0;
            if (player.Position > FINISH_LINE) player.Position = FINISH_LINE;
        }
    }
}