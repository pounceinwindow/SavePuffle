using GravityFalls.Shared;
using System.Net.Sockets;
using System.Text.Json;

namespace GravityFalls.Server.Core
{
    public class ClientSession
    {
        public int Id { get; }
        public string Nickname { get; set; } = "Unknown";
        public bool IsReady { get; set; }
        public bool IsConnected => _client.Connected;

        public int Position { get; set; } = 0;
        public bool HasWaddles { get; set; } = false;
        public HeroType Hero { get; set; } = HeroType.Dipper;
        public int HelpTokens { get; set; } = 0;
        public int MischiefTokens { get; set; } = 0;
        public bool SkipNextTurn { get; set; } = false;

        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private readonly GameServer _server;

        public ClientSession(TcpClient client, GameServer server, int id)
        {
            _client = client;
            _server = server;
            _stream = client.GetStream();
            Id = id;
        }

        public void Send(byte[] data)
        {
            if (!IsConnected) return;
            try { _stream.Write(data); } catch { /* ignore */ }
        }

        public async Task ProcessLoop()
        {
            try
            {
                while (IsConnected)
                {
                    byte[] lenBuf = new byte[4];
                    bool ok = await ReadExactAsync(_stream, lenBuf);
                    if (!ok) break;

                    int length = BitConverter.ToInt32(lenBuf, 0);
                    if (length <= 0 || length > 1024 * 1024)
                        throw new InvalidOperationException($"Bad packet length: {length}");

                    byte[] body = new byte[length];
                    ok = await ReadExactAsync(_stream, body);
                    if (!ok) break;

                    OpCode op = (OpCode)body[0];
                    string json = length > 1 ? CryptoHelper.Decrypt(body[1..]) : "";

                    HandlePacket(op, json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client {Id} error: {ex.Message}");
            }
            finally
            {
                _server.RemoveClient(this);
                try { _client.Close(); } catch { }
            }
        }

        private static async Task<bool> ReadExactAsync(NetworkStream stream, byte[] buffer)
        {
            int total = 0;
            while (total < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer, total, buffer.Length - total);
                if (read == 0) return false;
                total += read;
            }
            return true;
        }

        private void HandlePacket(OpCode op, string json)
        {
            switch (op)
            {
                case OpCode.Login:
                    {
                        var login = JsonSerializer.Deserialize<LoginDto>(json);
                        Nickname = string.IsNullOrWhiteSpace(login?.Nickname) ? $"Player{Id}" : login!.Nickname.Trim();
                        _server.BroadcastLobbySnapshot();
                        break;
                    }
                case OpCode.SelectHero:
                    {
                        var dto = JsonSerializer.Deserialize<SelectHeroDto>(json);
                        if (dto != null)
                        {
                            Hero = dto.Hero;
                            _server.BroadcastLobbySnapshot();
                        }
                        break;
                    }
                case OpCode.ToggleReady:
                    {
                        IsReady = !IsReady;
                        _server.BroadcastLobbySnapshot();
                        _server.CheckAllReady();
                        break;
                    }
                case OpCode.RollDice:
                    {
                        RollDiceDto dto;
                        try
                        {
                            dto = string.IsNullOrWhiteSpace(json)
                                ? new RollDiceDto()
                                : (JsonSerializer.Deserialize<RollDiceDto>(json) ?? new RollDiceDto());
                        }
                        catch
                        {
                            dto = new RollDiceDto();
                        }

                        _server.GameLoop.HandleDiceRoll(this, dto);
                        break;
                    }
                case OpCode.Exchange:
                    _server.GameLoop.TryExchange(this);
                    break;
            }
        }
    }
}
