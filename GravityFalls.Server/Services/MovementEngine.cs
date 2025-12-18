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

            _map[0] = TileType.Start;
            _map[5] = TileType.ArrowBlue;
            _map[3] = TileType.Help;
            _map[8] = TileType.Honey;
            _map[12] = TileType.ArrowRed;
            _map[16] = TileType.Portal;
            _map[20] = TileType.Mischief;
            _map[23] = TileType.Help;
            _map[26] = TileType.Trap;
            _map[28] = TileType.Honey;
            _map[30] = TileType.Finish;
        }

        public void ProcessMove(ClientSession player, int diceRoll, List<ClientSession> allPlayers)
        {
            int stepCap = player.HasWaddles ? 3 : 6;
            if (player.NextStepLimit.HasValue) stepCap = Math.Min(stepCap, player.NextStepLimit.Value);

            int actualSteps = Math.Min(diceRoll, stepCap);

            if (player.Hero == HeroType.Dipper && !player.HasWaddles)
            {
                actualSteps = Math.Min(actualSteps + 1, stepCap);
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

            HandleTile(player, allPlayers);

            if (player.Position < 0) player.Position = 0;
            if (player.Position > FINISH_LINE) player.Position = FINISH_LINE;

            if (player.Hero == HeroType.Soos)
            {
                player.NextStepLimit = null; // помощник избавляется от дебаффов
            }
        }

        private void HandleTile(ClientSession player, List<ClientSession> allPlayers)
        {
            switch (_map[player.Position])
            {
                case TileType.ArrowBlue:
                    player.Position = Math.Min(player.Position + 2, FINISH_LINE);
                    break;
                case TileType.ArrowRed:
                    int penalty = player.Hero == HeroType.Stan ? 1 : 2;
                    player.Position = Math.Max(0, player.Position - penalty);
                    break;
                case TileType.Mischief:
                    var last = allPlayers
                        .Where(p => p.Id != player.Id)
                        .OrderBy(p => p.Position)
                        .FirstOrDefault();
                    if (last != null)
                    {
                        int temp = player.Position;
                        player.Position = last.Position;
                        last.Position = temp;
                    }
                    break;
                case TileType.Help:
                    player.NextStepLimit = null;
                    player.Position = Math.Min(player.Position + 1, FINISH_LINE);
                    break;
                case TileType.Honey:
                    if (player.Hero == HeroType.Mabel)
                    {
                        player.NextStepLimit = null; // очаровала липкую ловушку
                    }
                    else
                    {
                        player.NextStepLimit = 2;
                    }
                    break;
                case TileType.Portal:
                    int advance = player.Hero == HeroType.Dipper ? 4 : 3;
                    player.Position = Math.Min(player.Position + advance, FINISH_LINE);
                    break;
                case TileType.Trap:
                    int setback = player.Hero == HeroType.Stan ? 2 : player.Hero == HeroType.Mabel ? 1 : 3;
                    player.Position = Math.Max(0, player.Position - setback);
                    player.NextStepLimit = player.Hero == HeroType.Wendy ? 2 : 3;
                    break;
            }
        }
    }
}