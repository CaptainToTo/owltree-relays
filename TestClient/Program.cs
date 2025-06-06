﻿using OwlTree;
using OwlTree.Matchmaking;

/// Example test client to test relay service

public static class Program
{
    public static bool active = true;

    public static void Main(string[] args)
    {
        if (!Directory.Exists("logs"))
            Directory.CreateDirectory("logs");

        if (args.Length == 3 || args.Length == 4)
            UseMatchmaking(args);
        else if (args.Length == 5)
            DirectConnect(args);
        else
        {
            Console.WriteLine("Matchmaking Usage: dotnet run [appId] [endpoint] [client/host/pub] [sessionId]");
            Console.WriteLine("Direct Connect Usage: dotnet run [appId] [sessionId] [ip address] [tcp] [udp]");
        }

        while (active)
            Thread.Sleep(100);
    }

    private static void DirectConnect(string[] args)
    {
        var rand = new Random();
        var logId = (rand.Next() % 900) + 100;
        var logFile = $"logs/client{logId}.log";
        Console.WriteLine("client log id: " + logId.ToString());

        var client = new Connection(new Connection.Args
        {
            role = NetRole.Client,
            serverAddr = args[2],
            tcpPort = int.Parse(args[3]),
            udpPort = int.Parse(args[4]),
            appId = args[0],
            sessionId = args[1],
            logger = (str) => File.AppendAllText(logFile, str),
            verbosity = Logger.Includes().All()
        });

        client.OnReady += (id) =>
        {
            if (client.IsHost)
                Console.WriteLine("assigned as host");
            Console.WriteLine("assigned client id: " + id.ToString());
        };

        client.OnClientConnected += (id) =>
        {
            Console.WriteLine($"client {id} connected");
        };

        client.OnClientDisconnected += (id) =>
        {
            Console.WriteLine($"client {id} disconnected");
        };

        client.OnHostMigration += (id) =>
        {
            if (client.IsHost)
                Console.WriteLine("you are now the host");
            else
                Console.WriteLine($"client {id} assigned as new host");
        };

        client.OnLocalDisconnect += (id) =>
        {
            Console.WriteLine("disconnected");
        };

        while (client.IsActive)
        {
            client.ExecuteQueue();
            Thread.Sleep(client.TickRate);
        }

        client.Disconnect();

        active = false;
    }

    private static async void UseMatchmaking(string[] args)
    {
        var rand = new Random();
        var logId = (rand.Next() % 900) + 100;
        var logFile = $"logs/client{logId}.log";
        Console.WriteLine("client log id: " + logId.ToString());

        var apiClient = new MatchmakingClient(args[1]);

        Connection client = null;
        HostReporter reporter = null;

        if (args[2] == "host")
        {
            var response = await apiClient.CreateSession(new SessionCreationRequest
            {
                appId = args[0],
                sessionId = logId.ToString(),
                maxClients = 6,
                migratable = false,
                simulationSystem = SimulationSystem.None,
                tickRate = 20
            });

            if (response.RequestFailed)
            {
                Console.WriteLine("failed to request relay server");
                Environment.Exit(0);
            }

            client = new Connection(new Connection.Args
            {
                role = NetRole.Host,
                serverAddr = response.serverAddr,
                tcpPort = response.tcpPort,
                udpPort = response.udpPort,
                appId = args[0],
                sessionId = logId.ToString(),
                maxClients = 6,
                migratable = false,
                simulationSystem = SimulationSystem.None,
                simulationTickRate = 20,
                logger = (str) => File.AppendAllText(logFile, str),
                verbosity = Logger.Includes().All()
            });
        }
        else if (args[2] == "pub")
        {
            client = new Connection(new Connection.Args
            {
                role = NetRole.Server,
                serverAddr = "0.0.0.0",
                tcpPort = 0,
                udpPort = 0,
                appId = args[0],
                sessionId = logId.ToString(),
                maxClients = 6,
                simulationSystem = SimulationSystem.None,
                simulationTickRate = 20,
                logger = (str) => File.AppendAllText(logFile, str),
                verbosity = Logger.Includes().All()
            });

            var response = await apiClient.PublishSession(new SessionPublishRequest
            {
                appId = client.AppId.Id,
                sessionId = client.SessionId.Id,
                maxPlayers = client.MaxClients,
                tcpPort = client.ServerTcpPort,
                udpPort = client.ServerUdpPort,
                simulationSystem = client.SimulationSystem,
                tickRate = client.TickRate
            });

            if (response.RequestFailed)
            {
                Console.WriteLine("failed to publish session");
                Environment.Exit(0);
            }

            reporter = new HostReporter(response.reportingEndpoint, client.AppId.Id, client.SessionId.Id, 10000);

            for (int i = 0; i < 10; i++)
            {
                await reporter.Connect();
                if (reporter.Connected)
                    break;
            }

            if (!reporter.Connected)
            {
                Console.WriteLine("failed to connect to host reporting endpoint");
                Environment.Exit(0);
            }

            client.OnClientConnected += (id) => reporter.ReportClientCount(client.ClientCount);
            client.OnClientDisconnected += (id) => reporter.ReportClientCount(client.ClientCount);

            client.OnLocalDisconnect += (id) => reporter.ReportShutdown();
        }
        else
        {
            var response = await apiClient.GetSession(new SessionDataRequest
            {
                appId = args[0],
                sessionId = args[3]
            });

            if (response.RequestFailed)
            {
                Console.WriteLine("failed to request server");
                Environment.Exit(0);
            }

            client = new Connection(new Connection.Args
            {
                role = NetRole.Client,
                serverAddr = response.serverAddr,
                tcpPort = response.tcpPort,
                udpPort = response.udpPort,
                appId = args[0],
                sessionId = args[3],
                maxClients = response.maxClients,
                migratable = response.migratable,
                simulationSystem = response.simulationSystem,
                simulationTickRate = response.tickRate,
                logger = (str) => File.AppendAllText(logFile, str),
                verbosity = Logger.Includes().All()
            });
        }

        client.OnReady += (id) =>
        {
            if (client.IsHost)
                Console.WriteLine("assigned as host");
            Console.WriteLine("assigned client id: " + id.ToString());
        };

        client.OnClientConnected += (id) =>
        {
            Console.WriteLine($"client {id} connected");
        };

        client.OnClientDisconnected += (id) =>
        {
            Console.WriteLine($"client {id} disconnected");
        };

        client.OnHostMigration += (id) =>
        {
            if (client.IsHost)
                Console.WriteLine("you are now the host");
            else
                Console.WriteLine($"client {id} assigned as new host");
        };

        client.OnLocalDisconnect += (id) =>
        {
            Console.WriteLine("disconnected");
        };

        while (client.IsActive)
        {
            client.ExecuteQueue();

            if (reporter != null && reporter.TimeForPing)
                reporter.Ping();

            Thread.Sleep(client.TickRate);
        }

        client.Disconnect();

        active = false;
    }
}
