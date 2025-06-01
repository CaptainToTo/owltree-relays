using System.Collections.Concurrent;
using System.Net;
using OwlTree;
using OwlTree.Matchmaking;

public static class Program
{
    public static SessionStore sessions;

    public static void Main(string[] args)
    {
        if (args.Length != 5)
        {
            Console.WriteLine("Usage: dotnet run [api endpoint] [reporting local] [reporting remote] [clear rate] [drop threshold]");
            return;
        }

        var apiEndpoint = args[0];
        var reportingLocal = args[1];
        var reportingRemote = args[2];
        var clearRate = int.Parse(args[3]);
        var dropThreshold = int.Parse(args[4]);

        Console.WriteLine("matchmaking endpoint listening on: " + apiEndpoint);
        Console.WriteLine("host reporting endpoint listening on: " + reportingLocal);

        if (!Directory.Exists("logs"))
            Directory.CreateDirectory("logs");

        var endpoint = new MatchmakingEndpoint(
            apiEndpoint,
            // callbacks to handle http request
            createSession: MatchmakingCallbacks.CreateSession,
            getSession: MatchmakingCallbacks.GetSession,

            // not used in simple relay service
            publishSession: MatchmakingCallbacks.PublishSession,
            getTicket: MatchmakingCallbacks.GetTicket,
            getTicketStatus: MatchmakingCallbacks.GetTicketStatus
        );
        sessions = new SessionStore(reportingLocal, reportingRemote, clearRate, dropThreshold);

        endpoint.Start(); // endpoint starts its own async task
        HandleCommands(); // cli in main thread
        endpoint.Close();
        sessions.Stop();
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
                case "s":
                case "sessions":
                    Commands.SessionList(sessions);
                    break;
                case "q":
                case "quit":
                    quit = true;
                    break;
                case "r":
                case "remove":
                    if (tokens.Length != 3)
                    {
                        Console.WriteLine("an app and session id must be provided");
                        break;
                    }
                    Commands.Remove(sessions, tokens[1], tokens[2]);
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