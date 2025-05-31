using System.Collections.Concurrent;
using System.Net;
using OwlTree;
using OwlTree.Matchmaking;


/// <summary>
/// Program contains 3 primary threads. A main thread that handles CLI input.
/// An HTTP thread created by the matchmaking endpoint to handle requests.
/// And a relay thread created by the relay manager that will continuously execute queues.
/// Each relay also manages its own recv/send threads. Each relay will also produce log files
/// named after their session id.
/// </summary>

public static class Program
{
    public static RelayManager relays;
    public static string ip = "*";

    public static void Main(string[] args)
    {

        if (args.Length != 5)
        {
            Console.WriteLine("Usage: dotnet run [ip] [api ip] [api port]");
            return;
        }

        ip = args[0];
        var apiIp = args[1];
        var matchmakingPort = int.Parse(args[2]);

        var domain = "http://" + apiIp + ":" + matchmakingPort + "/";

        Console.WriteLine("matchmaking endpoint listening on: " + domain);

        if (!Directory.Exists("logs"))
            Directory.CreateDirectory("logs");

        var endpoint = new MatchmakingEndpoint(domain)
        {

        };
        relays = new RelayManager(100);

        endpoint.Start();
        HandleCommands();
        endpoint.Close();
        relays.DisconnectAll();
    }

    // main thread handles CLI
    public static void HandleCommands()
    {
        while (true)
        {
            Console.Write("input command (h): ");
            var com = Console.ReadLine();
            if (com == null)
                continue;

            var tokens = com.Split(' ');

            var quit = false;

            switch (tokens[0])
            {
                case "r":
                case "relays":
                    Commands.RelayList(relays);
                    break;
                case "p":
                case "players":
                    if (tokens.Length != 3)
                    {
                        Console.WriteLine("an app and session id must be provided\n");
                        break;
                    }
                    var relay = relays!.Get(tokens[1], tokens[2]);
                    if (relay == null)
                    {
                        Console.WriteLine("no relay has that app and session id\n");
                        break;
                    }
                    Commands.PlayerList(relay);
                    break;
                case "q":
                case "quit":
                    quit = true;
                    break;
                case "ping":
                    if (tokens.Length != 4)
                    {
                        Console.WriteLine("an app, session, and client id must be provided\n");
                        break;
                    }
                    relay = relays!.Get(tokens[1], tokens[2]);
                    if (relay == null)
                    {
                        Console.WriteLine("no relay has that app and session id\n");
                        break;
                    }
                    Commands.Ping(tokens[2], relay);
                    break;
                case "d":
                case "disconnect":
                    if (tokens.Length != 4)
                    {
                        Console.WriteLine("an app, session, and client id must be provided\n");
                        break;
                    }
                    relay = relays!.Get(tokens[1], tokens[2]);
                    if (relay == null)
                    {
                        Console.WriteLine("no relay has that app and session id\n");
                        break;
                    }
                    Commands.Disconnect(tokens[2], relay);
                    break;
                case "h":
                case "help":
                default:
                    Commands.Help();
                    break;
            }

            if (quit)
            {
                break;
            }
        }
    }
}