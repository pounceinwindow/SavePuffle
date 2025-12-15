namespace GravityFalls.Shared
{
    // Login
    public class LoginDto 
    { 
        public string Email { get; set; } = ""; 
        public string Nickname { get; set; } = ""; 
    }

    // Lobby
    public class LobbyStateDto
    {
        public List<LobbySlotDto> Slots { get; set; } = new();
        public string RoomCode { get; set; } = "";
    }

    public class LobbySlotDto
    {
        public int SlotIndex { get; set; }
        public string DisplayText { get; set; } = "";
        public bool IsReady { get; set; }
        public bool IsEmpty { get; set; }
    }

    // Game
    public class GameStateDto
    {
        public List<PlayerStateDto> Players { get; set; } = new();
        public int CurrentTurnPlayerId { get; set; }
        public int WaddlesOwnerId { get; set; } // -1 if on board
    }

    public class PlayerStateDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Position { get; set; }
        public bool HasWaddles { get; set; }
    }
    
    public class DiceResultDto
    {
        public int Value { get; set; }
        public int PlayerId { get; set; }
    }
    
    public class GameOverDto { public string WinnerName { get; set; } = ""; }
}