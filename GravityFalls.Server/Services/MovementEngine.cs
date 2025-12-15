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
            for(int i=0; i<=FINISH_LINE; i++) _map.Add(TileType.Empty);
            
            // Add logical traps/boosts (Simple Example)
            _map[5] = TileType.ArrowBlue; // Boost
            _map[12] = TileType.ArrowRed; // Trap
            _map[20] = TileType.Mischief;
            _map[30] = TileType.Finish;
        }

        public void ProcessMove(ClientSession player, int diceRoll, List<ClientSession> allPlayers)
        {
            // RULE 1: Waddles Speed Limit
            int actualSteps = diceRoll;
            if (player.HasWaddles)
            {
                actualSteps = Math.Min(diceRoll, 3);
            }

            int startPos = player.Position;
            int endPos = startPos + actualSteps;

            // RULE 2: Overtaking Waddles (Stealing)
            // If someone else has Waddles, and I pass them, I take it.
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
            // Logic: Picking up Waddles from board (e.g. at tile 10)
            else if (allPlayers.All(p => !p.HasWaddles) && endPos >= 10) 
            {
                 player.HasWaddles = true;
            }

            // Apply Move
            player.Position = Math.Min(endPos, FINISH_LINE);

            // RULE 3: Tile Effects (Simplified)
            // If arrow, move again.
            if (_map[player.Position] == TileType.ArrowBlue) player.Position += 2;
            if (_map[player.Position] == TileType.ArrowRed) player.Position -= 2;
            
            // Limit bounds
            if (player.Position < 0) player.Position = 0;
            if (player.Position > FINISH_LINE) player.Position = FINISH_LINE;
        }
    }
}