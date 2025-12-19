namespace GravityFalls.Shared
{
    public enum OpCode : byte
    {
        Login = 1,
        LobbyUpdate = 2,
        ToggleReady = 3,
        StartGame = 4,
        SelectHero = 5,

        RollDice = 10,
        DiceResult = 11,
        GameState = 12,
        GameOver = 13,
        GameEvent = 14,
        Exchange = 15,

        Error = 255
    }

    public enum HeroType : byte { Dipper = 0, Mabel = 1, Stan = 2, Soos = 3, Wendy = 4 }

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
