namespace GravityFalls.Shared
{
    public enum OpCode : byte
    {
        Login = 1,
        LobbyUpdate = 2,
        ToggleReady = 3,
        StartGame = 4,

        RollDice = 10,
        DiceResult = 11,
        GameState = 12,
        GameOver = 13,

        Error = 255
    }

    public enum HeroType { Dipper, Mabel, Stan, Soos, Wendy }

    public enum TileType
    {
        Empty,
        Start,
        Finish,
        ArrowRed,
        ArrowBlue,
        Mischief,
        Help
    }
}