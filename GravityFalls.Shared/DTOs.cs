namespace GravityFalls.Shared
{
    public class LoginDto
    {
        public string Nickname { get; set; } = "";
    }

    public class SelectHeroDto
    {
        public HeroType Hero { get; set; }
    }

    public class ExchangeDto
    {
    }

    public enum GameEventKind : byte
    {
        Info = 0,
        Good = 1,
        Bad = 2,
    }

    public class GameEventDto
    {
        public GameEventKind Kind { get; set; } = GameEventKind.Info;
        public string Message { get; set; } = "";
    }

    public class LobbyStateDto
    {
        public List<LobbySlotDto> Slots { get; set; } = new();
        public string RoomCode { get; set; } = "";
    }

    public class LobbySlotDto
    {
        public int SlotIndex { get; set; }
        public int PlayerId { get; set; } = -1;
        public string Nickname { get; set; } = "";
        public HeroType Hero { get; set; } = HeroType.Dipper;
        public bool IsReady { get; set; }
        public bool IsEmpty { get; set; }
    }

    public class GameStateDto
    {
        public List<PlayerStateDto> Players { get; set; } = new();
        public int CurrentTurnPlayerId { get; set; }

        public int WaddlesPosition { get; set; } = -1;

        public int WaddlesCarrierId { get; set; } = -1;

        public int LastDiceValue { get; set; } = 0;
    }

    public class PlayerStateDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public HeroType Hero { get; set; } = HeroType.Dipper;
        public int Position { get; set; }

        public int HelpTokens { get; set; } = 0;
        public int MischiefTokens { get; set; } = 0;

        public bool SkipNextTurn { get; set; } = false;
        public bool HasWaddles { get; set; } = false;
    }

    public class DiceResultDto
    {
        public int Value { get; set; }
        public int PlayerId { get; set; }
    }

    public class GameOverDto
    {
        public string WinnerName { get; set; } = "";
    }
}
