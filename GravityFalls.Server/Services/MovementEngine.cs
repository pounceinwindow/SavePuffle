using GravityFalls.Server.Core;
using GravityFalls.Shared;
using System.Linq;

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

            _map[0] = TileType.Start;
            _map[2] = TileType.Help;
            _map[4] = TileType.Trap;
            _map[5] = TileType.ArrowBlue;
            _map[7] = TileType.Treasure;
            _map[9] = TileType.ArrowRed;
            _map[12] = TileType.Mischief;
            _map[14] = TileType.Help;
            _map[16] = TileType.Trap;
            _map[18] = TileType.ArrowBlue;
            _map[20] = TileType.Mischief;
            _map[22] = TileType.Treasure;
            _map[24] = TileType.ArrowRed;
            _map[26] = TileType.Help;
            _map[28] = TileType.Trap;
            _map[30] = TileType.Finish;
        }

        public void ProcessMove(ClientSession player, int diceRoll, List<ClientSession> allPlayers)
        {
            int actualSteps = diceRoll;

            if (player.Hero == HeroType.Wendy && diceRoll >= 5)
            {
                actualSteps += 1;
            }

            if (player.HasWaddles)
            {
                actualSteps = Math.Min(actualSteps, 3);
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

            ApplyTileEffect(player, allPlayers);

            if (player.Position < 0) player.Position = 0;
            if (player.Position > FINISH_LINE) player.Position = FINISH_LINE;
        }

        private void ApplyTileEffect(ClientSession player, List<ClientSession> allPlayers)
        {
            switch (_map[player.Position])
            {
                case TileType.ArrowBlue:
                    player.Position = Math.Min(player.Position + 2, FINISH_LINE);
                    break;
                case TileType.ArrowRed:
                    player.Position = Math.Max(player.Position - 2, 0);
                    player.HasWaddles = false;
                    break;
                case TileType.Help:
                    int helpBoost = player.Hero == HeroType.Dipper ? 2 : 1;
                    player.Position = Math.Min(player.Position + helpBoost, FINISH_LINE);
                    break;
                case TileType.Mischief:
                    int mischiefPenalty = player.Hero == HeroType.Mabel ? 1 : 2;
                    player.Position = Math.Max(player.Position - mischiefPenalty, 0);
                    if (player.HasWaddles && player.Hero != HeroType.Mabel)
                    {
                        player.HasWaddles = false;
                    }
                    break;
                case TileType.Treasure:
                    int treasureBoost = player.Hero == HeroType.Stan ? 2 : 1;
                    player.Position = Math.Min(player.Position + treasureBoost, FINISH_LINE);
                    if (!player.HasWaddles && allPlayers.All(p => !p.HasWaddles) && player.Position >= 10)
                    {
                        player.HasWaddles = true;
                    }
                    break;
                case TileType.Trap:
                    int trapPenalty = player.Hero == HeroType.Soos ? 1 : 3;
                    player.Position = Math.Max(player.Position - trapPenalty, 0);
                    player.HasWaddles = false;
                    break;
            }
        }
    }
}