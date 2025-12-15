using GravityFalls.Server.Core;

Console.Title = "Gravity Falls Server";
Console.WriteLine("Starting Server...");

var server = new GameServer();

await server.StartAsync(8888);