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

        private TcpClient _client;
        private NetworkStream _stream;
        private GameServer _server;

        public ClientSession(TcpClient client, GameServer server, int id)
        {
            _client = client;
            _server = server;
            _stream = client.GetStream();
            Id = id;
        }

        public void Send(byte[] data)
        {
            if (IsConnected) try { _stream.Write(data); } catch { }
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
                    string json = "";
                    if (length > 1) json = CryptoHelper.Decrypt(body[1..]);

                    HandlePacket(op, json);
                }
            }
            catch { Console.WriteLine($"Client {Id} Error"); }
            finally { _server.RemoveClient(this); _client.Close(); }
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
                    var login = JsonSerializer.Deserialize<LoginDto>(json);
                    Nickname = string.IsNullOrWhiteSpace(login?.Nickname) ? "Unknown" : login!.Nickname;
                    _server.BroadcastLobbySnapshot();
                    break;
                case OpCode.SetHero:
                    var heroDto = JsonSerializer.Deserialize<HeroSelectionDto>(json);
                    if (heroDto != null) Hero = heroDto.Hero;
                    _server.BroadcastLobbySnapshot();
                    break;
                case OpCode.ToggleReady:
                    IsReady = !IsReady;
                    _server.BroadcastLobbySnapshot();
                    _server.CheckAllReady();
                    break;
                case OpCode.RollDice:
                    _server.GameLoop.HandleDiceRoll(this);
                    break;
            }
        }
    }
}