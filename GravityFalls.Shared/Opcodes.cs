namespace GravityFalls.Shared
{
    /// <summary>
    /// Custom socket protocol opcodes.
    /// Packet format:
    /// [4 bytes length (LE)] [1 byte OpCode] [AES-encrypted JSON]
    /// </summary>
    public enum OpCode : byte
    {
        // Lobby / session
        Login = 1,
        LobbyUpdate = 2,
        ToggleReady = 3,
        StartGame = 4,
        SelectHero = 5,

        // Game
        RollDice = 10,
        DiceResult = 11,
        GameState = 12,
        GameOver = 13,
        GameEvent = 14,
        Exchange = 15,

        Error = 255
    }

    /// <summary>Heroes from the rulebook.</summary>
    public enum HeroType : byte { Dipper = 0, Mabel = 1, Stan = 2, Soos = 3, Wendy = 4 }

    /// <summary>Board tiles (simplified rule variant A: tokens + immediate effects).</summary>
    public enum TileType : byte
    {
        Empty = 0,
        Start = 1,
        Finish = 2,

        ArrowRed = 10,
        ArrowBlue = 11,

        Mischief = 20,
        Help = 21,

        Exchange = 30,
        ExtraTurn = 31,
        SkipTurn = 32,
        DiscardHelp = 33,

        Signpost = 40,
        Totem = 41,
    }
}
