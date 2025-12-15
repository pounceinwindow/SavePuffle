using GravityFalls.Server.Core;

Console.Title = "Gravity Falls Server";
Console.WriteLine("Starting Server...");

// 1. Initialize Server
var server = new GameServer();

// 2. Start Listening on Port 8888
await server.StartAsync(8888);