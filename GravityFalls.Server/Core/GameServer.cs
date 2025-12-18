using GravityFalls.Server.Services;
using GravityFalls.Shared;
using System.Net;
using System.Net.Sockets;

namespace GravityFalls.Server.Core
{
    public class GameServer
    {
        private readonly TcpListener _listener;
        private readonly List<ClientSession> _clients = new();
        public GameLoopService GameLoop { get; }

        public GameServer(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            GameLoop = new GameLoopService(this);
        }

        public async Task StartAsync(CancellationToken ct = default)
        {
            _listener.Start();
            Console.WriteLine($"[Server] Listening...");

            while (!ct.IsCancellationRequested)
            {
                var tcpClient = await _listener.AcceptTcpClientAsync(ct);

                if (_clients.Count >= 4 || GameLoop.IsGameStarted)
                {
                    Console.WriteLine("Connection rejected (full or game started)");
                    try { tcpClient.Close(); } catch { }
                    continue;
                }

                var session = new ClientSession(tcpClient, this, _clients.Count);
                _clients.Add(session);
                BroadcastLobbySnapshot();
                _ = Task.Run(session.ProcessLoop, ct);
            }
        }

        public void Broadcast(byte[] packet)
        {
            foreach (var c in _clients.Where(x => x.IsConnected))
                c.Send(packet);
        }

        public void SendTo(int playerId, byte[] packet)
        {
            var c = _clients.FirstOrDefault(x => x.Id == playerId);
            c?.Send(packet);
        }

        public IReadOnlyList<ClientSession> GetClientsSnapshot() => _clients.ToList();

        public void BroadcastLobbySnapshot()
        {
            var snapshot = new LobbyStateDto { RoomCode = "DP-P9CR" };

            for (int i = 0; i < 4; i++)
            {
                var c = _clients.FirstOrDefault(x => x.Id == i);
                if (c != null)
                {
                    snapshot.Slots.Add(new LobbySlotDto
                    {
                        SlotIndex = i,
                        PlayerId = c.Id,
                        Nickname = c.Nickname,
                        Hero = c.Hero,
                        IsReady = c.IsReady,
                        IsEmpty = false
                    });
                }
                else
                {
                    snapshot.Slots.Add(new LobbySlotDto
                    {
                        SlotIndex = i,
                        PlayerId = -1,
                        Nickname = "",
                        Hero = HeroType.Dipper,
                        IsReady = false,
                        IsEmpty = true
                    });
                }
            }

            Broadcast(Packet.Serialize(OpCode.LobbyUpdate, snapshot));
        }

        public void CheckAllReady()
        {
            if (_clients.Count == 4 && _clients.All(c => c.IsReady))
            {
                Console.WriteLine("All ready -> start game");
                Broadcast(Packet.Serialize(OpCode.StartGame, new object()));
                GameLoop.StartGame(_clients);
            }
        }

        public void RemoveClient(ClientSession session)
        {
            _clients.Remove(session);
            BroadcastLobbySnapshot();
        }
    }
}
