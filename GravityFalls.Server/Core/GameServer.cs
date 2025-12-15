using System.Net;
using System.Net.Sockets;
using GravityFalls.Server.Services;
using GravityFalls.Shared;

namespace GravityFalls.Server.Core
{
    public class GameServer
    {
        private TcpListener _listener;
        private List<ClientSession> _clients = new();
        public GameLoopService GameLoop { get; private set; }

        public GameServer()
        {
            // Initialize the Brain
            GameLoop = new GameLoopService(this);
        }

        public async Task StartAsync(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            Console.WriteLine($"[Server] Listening on port {port}...");

            while (true)
            {
                var tcpClient = await _listener.AcceptTcpClientAsync();
                
                if (_clients.Count >= 4 || GameLoop.IsGameStarted)
                {
                    Console.WriteLine("Connection rejected (Full or Started)");
                    tcpClient.Close();
                    continue;
                }

                var session = new ClientSession(tcpClient, this, _clients.Count);
                _clients.Add(session);
                _ = Task.Run(session.ProcessLoop);
            }
        }

        public void Broadcast(byte[] packet)
        {
            foreach (var c in _clients.Where(x => x.IsConnected)) c.Send(packet);
        }

        public void BroadcastLobbySnapshot()
        {
            var snapshot = new LobbyStateDto { RoomCode = "DP-P9CR" };
            for (int i = 0; i < 4; i++)
            {
                if (i < _clients.Count)
                {
                    var c = _clients[i];
                    snapshot.Slots.Add(new LobbySlotDto { 
                        SlotIndex = i, 
                        DisplayText = c.Nickname, 
                        IsReady = c.IsReady, 
                        IsEmpty = false 
                    });
                }
                else
                {
                    snapshot.Slots.Add(new LobbySlotDto { SlotIndex = i, DisplayText = "Empty", IsEmpty = true });
                }
            }
            Broadcast(Packet.Serialize(OpCode.LobbyUpdate, snapshot));
        }

        public void CheckAllReady()
        {
            if (_clients.Count > 0 && _clients.All(c => c.IsReady))
            {
                Console.WriteLine("All Ready! Starting Game...");
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