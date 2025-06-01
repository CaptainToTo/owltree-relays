using System.Net;
using OwlTree;
using OwlTree.Matchmaking;

public static class MatchmakingCallbacks
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public static async Task<SessionPublishResponse> PublishSession(IPAddress source, SessionPublishRequest request)
    {
        var sessions = Program.sessions;

        if (sessions.Contains(request.appId, request.sessionId))
            return SessionPublishResponse.RequestRejected;

        var data = new SessionData(
            appId: request.appId,
            sessionId: request.sessionId,
            ip: request.hostAddr,
            tcpPort: request.tcpPort,
            udpPort: request.udpPort,
            maxClients: request.maxPlayers,
            simulationSystem: request.simulationSystem,
            tickRate: request.tickRate
        );

        sessions.Add(data);

        return new SessionPublishResponse
        {
            responseCode = ResponseCodes.RequestAccepted,
            reportingEndpoint = sessions.reportingRemote
        };
    }


    public static async Task<SessionDataResponse> GetSession(IPAddress source, SessionDataRequest request)
    {
        var sessions = Program.sessions;

        if (sessions == null)
            return SessionDataResponse.RequestRejected;


        if (sessions.TryGet(request.appId, request.sessionId, out var data) && data.HostConnected)
        {
            return new SessionDataResponse
            {
                responseCode = ResponseCodes.RequestAccepted,
                serverAddr = data.ip,
                tcpPort = data.tcpPort,
                udpPort = data.udpPort,
                maxClients = data.maxClients,
                migratable = false,
                simulationSystem = data.simulationSystem,
                tickRate = data.tickRate
            };
        }

        return SessionDataResponse.RequestRejected;
    }






    public static async Task<SessionCreationResponse> CreateSession(IPAddress source, SessionCreationRequest request)
    {
        return SessionCreationResponse.RequestRejected;
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