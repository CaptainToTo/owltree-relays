using System.Net;
using OwlTree;
using OwlTree.Matchmaking;

public static class MatchmakingCallbacks
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public static async Task<SessionCreationResponse> CreateSession(IPAddress source, SessionCreationRequest request)
    {
        var relays = Program.relays;

        if (relays == null)
            return SessionCreationResponse.RequestRejected;

        if (!relays.Contains(request.appId, request.sessionId) && !relays.IsFull)
        {
            var logFile = $"logs/relay-{request.sessionId}-{Timestamp.Now}.log";
            File.WriteAllText(logFile, "");

            var connection = relays.Add(new Connection.Args
            {
                appId = request.appId,
                sessionId = request.sessionId,
                role = NetRole.Relay,
                serverAddr = Program.ip,
                simulationSystem = request.simulationSystem,
                simulationTickRate = request.tickRate,
                tcpPort = 0,
                udpPort = 0,
                hostAddr = source.ToString(),
                maxClients = request.maxClients,
                migratable = request.migratable,
                logger = (str) => File.AppendAllText(logFile, str),
                verbosity = Logger.Includes().LogSeparators().LogTimestamp().ClientEvents().ConnectionAttempts().Exceptions()
            });

            connection.Log($"New Relay: {connection.SessionId} for App {connection.AppId}\nTCP: {connection.LocalTcpPort}\nUDP: {connection.LocalUdpPort}\nRequested Host: {source}");

            return new SessionCreationResponse
            {
                responseCode = ResponseCodes.RequestAccepted,
                serverAddr = Program.ip,
                tcpPort = connection.ServerTcpPort,
                udpPort = connection.ServerUdpPort
            };
        }

        return SessionCreationResponse.RequestRejected;
    }


    public static async Task<SessionDataResponse> GetSession(IPAddress source, SessionDataRequest request)
    {
        var relays = Program.relays;

        if (relays == null)
            return SessionDataResponse.RequestRejected;

        if (relays.TryGet(request.appId, request.sessionId, out var relay))
        {
            return new SessionDataResponse
            {
                responseCode = ResponseCodes.RequestAccepted,
                serverAddr = Program.ip,
                tcpPort = relay.ServerTcpPort,
                udpPort = relay.ServerUdpPort,
                maxClients = relay.MaxClients,
                migratable = relay.Migratable,
                simulationSystem = relay.SimulationSystem,
                tickRate = relay.TickRate
            };
        }

        return SessionDataResponse.RequestRejected;
    }







    public static async Task<SessionPublishResponse> PublishSession(IPAddress source, SessionPublishRequest request)
    {
        return SessionPublishResponse.RequestRejected;
    }

    public static async Task<MatchmakingTicketResponse> GetTicket(IPAddress source, MatchmakingTicketRequest request)
    {
        return MatchmakingTicketResponse.RequestRejected;
    }

    public static async Task<TicketStatusResponse> GetTicketStatus(IPAddress source, TicketStatusRequest request)
    {
        return TicketStatusResponse.RequestRejected;
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}