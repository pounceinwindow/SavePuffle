using GravityFalls.Server.Core;

Console.Title = "Gravity Falls Server";
Console.WriteLine("Starting Server...");

const int port = 8888;
var server = new GameServer(port);

await server.StartAsync();
