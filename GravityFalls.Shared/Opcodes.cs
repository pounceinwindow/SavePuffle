namespace GravityFalls.Shared
{
    public enum OpCode : byte
    {
        // Networking
        Login = 1,          
        LobbyUpdate = 2,    
        ToggleReady = 3,    
        StartGame = 4,
        
        // Gameplay
        RollDice = 10,      // Client -> Server
        DiceResult = 11,    // Server -> Client (Animation)
        GameState = 12,     // Server -> Client (Positions update)
        GameOver = 13,       // Server -> Client
        
        Error = 255
    }

    public enum HeroType { Dipper, Mabel, Stan, Soos, Wendy }

    public enum TileType 
    { 
        Empty, 
        Start, 
        Finish, 
        ArrowRed,   // Trap (Go back)
        ArrowBlue,  // Boost (Go forward)
        Mischief,   // Draw bad card
        Help        // Draw good card
    }
}