namespace SavePuffle.Models;

public sealed class PlayerInfo
{
    public string Nickname { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsHost { get; set; }
    public bool IsReady { get; set; }
}
