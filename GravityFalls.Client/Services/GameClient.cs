using GravityFalls.Shared;
using System.Net.Sockets;
using System.Text.Json;

namespace SavePuffle.Services;

public sealed class GameClient : IDisposable
{
    public static GameClient Instance { get; } = new();

    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _receiveCts;
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public bool IsConnected => _client?.Connected ?? false;

    public string Nickname { get; private set; } = "";

    public LobbyStateDto? LastLobbyState { get; private set; }
    public GameStateDto? LastGameState { get; private set; }
    public DiceResultDto? LastDiceResult { get; private set; }
    public GameOverDto? LastGameOver { get; private set; }

    public event Action<LobbyStateDto>? LobbyUpdated;
    public event Action? GameStarted;
    public event Action<GameStateDto>? GameStateUpdated;
    public event Action<DiceResultDto>? DiceResultReceived;
    public event Action<GameEventDto>? GameEventReceived;
    public event Action<GameOverDto>? GameOver;
    public event Action<string>? Error;
    public event Action? Disconnected;

    private GameClient() { }

    public async Task ConnectAsync(string host, int port, string nickname, CancellationToken ct = default)
    {
        Disconnect();

        Nickname = nickname;

        _client = new TcpClient();
        await _client.ConnectAsync(host, port, ct);
        _stream = _client.GetStream();

        _receiveCts = new CancellationTokenSource();
        _ = Task.Run(() => ReceiveLoopAsync(_receiveCts.Token));

        await SendAsync(OpCode.Login, new LoginDto { Nickname = nickname }, ct);
    }

    public Task ToggleReadyAsync(CancellationToken ct = default) => SendAsync(OpCode.ToggleReady, new { }, ct);
    public Task SelectHeroAsync(HeroType hero, CancellationToken ct = default) => SendAsync(OpCode.SelectHero, new SelectHeroDto { Hero = hero }, ct);
    public Task RollDiceAsync(CancellationToken ct = default) => SendAsync(OpCode.RollDice, new { }, ct);
    public Task ExchangeAsync(CancellationToken ct = default) => SendAsync(OpCode.Exchange, new ExchangeDto(), ct);

    public void Disconnect()
    {
        try { _receiveCts?.Cancel(); } catch { }
        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }

        _receiveCts?.Dispose();
        _receiveCts = null;
        _stream = null;
        _client = null;

        LastLobbyState = null;
        LastGameState = null;
        LastDiceResult = null;
        LastGameOver = null;
    }

    public void Dispose()
    {
        Disconnect();
        _sendLock.Dispose();
    }

    private async Task SendAsync(OpCode code, object data, CancellationToken ct)
    {
        if (_stream is null) throw new InvalidOperationException("Not connected");

        byte[] packet = Packet.Serialize(code, data);

        await _sendLock.WaitAsync(ct);
        try
        {
            await _stream.WriteAsync(packet, ct);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        try
        {
            if (_stream is null) return;

            var lenBuf = new byte[4];
            while (!ct.IsCancellationRequested)
            {
                bool ok = await ReadExactAsync(_stream, lenBuf, ct);
                if (!ok) break;

                int length = BitConverter.ToInt32(lenBuf, 0);
                if (length <= 0 || length > 1024 * 1024) 
                    throw new InvalidOperationException($"Bad packet length: {length}");

                var body = new byte[length];
                ok = await ReadExactAsync(_stream, body, ct);
                if (!ok) break;

                OpCode op = (OpCode)body[0];
                string json = length > 1 ? CryptoHelper.Decrypt(body[1..]) : "";

                HandleIncoming(op, json);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Error?.Invoke(ex.Message);
        }
        finally
        {
            Disconnected?.Invoke();
        }
    }

    private void HandleIncoming(OpCode op, string json)
    {
        switch (op)
        {
            case OpCode.LobbyUpdate:
                {
                    var lobby = JsonSerializer.Deserialize<LobbyStateDto>(json);
                    if (lobby != null)
                    {
                        LastLobbyState = lobby;
                        LobbyUpdated?.Invoke(lobby);
                    }
                    break;
                }
            case OpCode.StartGame:
                GameStarted?.Invoke();
                break;

            case OpCode.GameState:
                {
                    var state = JsonSerializer.Deserialize<GameStateDto>(json);
                    if (state != null)
                    {
                        LastGameState = state;
                        GameStateUpdated?.Invoke(state);
                    }
                    break;
                }
            case OpCode.DiceResult:
                {
                    var roll = JsonSerializer.Deserialize<DiceResultDto>(json);
                    if (roll != null)
                    {
                        LastDiceResult = roll;
                        DiceResultReceived?.Invoke(roll);
                    }
                    break;
                }
            case OpCode.GameEvent:
                {
                    var ev = JsonSerializer.Deserialize<GameEventDto>(json);
                    if (ev != null)
                        GameEventReceived?.Invoke(ev);
                    break;
                }
            case OpCode.GameOver:
                {
                    var over = JsonSerializer.Deserialize<GameOverDto>(json);
                    if (over != null)
                    {
                        LastGameOver = over;
                        GameOver?.Invoke(over);
                    }
                    break;
                }

            case OpCode.Error:
                Error?.Invoke(json);
                break;
        }
    }

    private static async Task<bool> ReadExactAsync(NetworkStream stream, byte[] buffer, CancellationToken ct)
    {
        int total = 0;
        while (total < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(total, buffer.Length - total), ct);
            if (read == 0) return false;
            total += read;
        }
        return true;
    }
}
