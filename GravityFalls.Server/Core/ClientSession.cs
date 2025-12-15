using System.Net.Sockets;
using System.Text.Json;
using GravityFalls.Shared;

namespace GravityFalls.Server.Core
{
    public class ClientSession
    {
        public int Id { get; }
        public string Nickname { get; set; } = "Unknown";
        public bool IsReady { get; set; }
        public bool IsConnected => _client.Connected;

        // Gameplay Data
        public int Position { get; set; } = 0;
        public bool HasWaddles { get; set; } = false;

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
            byte[] lenBuf = new byte[4];
            try
            {
                while (IsConnected)
                {
                    // 1. Read Length
                    int read = await _stream.ReadAsync(lenBuf, 0, 4);
                    if (read == 0) break;
                    int length = BitConverter.ToInt32(lenBuf, 0);

                    // 2. Read Body
                    byte[] body = new byte[length];
                    int totalRead = 0;
                    while (totalRead < length)
                        totalRead += await _stream.ReadAsync(body, totalRead, length - totalRead);

                    // 3. Process
                    OpCode op = (OpCode)body[0];
                    string json = "";
                    if (length > 1) json = CryptoHelper.Decrypt(body[1..]);

                    HandlePacket(op, json);
                }
            }
            catch { Console.WriteLine($"Client {Id} Error"); }
            finally { _server.RemoveClient(this); _client.Close(); }
        }

        private void HandlePacket(OpCode op, string json)
        {
            switch (op)
            {
                case OpCode.Login:
                    var login = JsonSerializer.Deserialize<LoginDto>(json);
                    Nickname = login.Nickname;
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