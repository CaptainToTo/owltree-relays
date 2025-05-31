using OwlTree;

namespace OwlTree.Matchmaking
{
    public static partial class Uris
    {
        public const string GetTicket = "/matchmaking";
        public const string TicketStatus = "/tickets";
    }

    /// <summary>
    /// Submit a matchmaking request to have a ticket queued
    /// for the given party of players
    /// </summary>
    public class MatchmakingTicketRequest : HttpRequest<MatchmakingTicketRequest>
    {
        public string appId { get; set; }
        public string partyId { get; set; }
        public string[] playerIds { get; set; }
    }

    /// <summary>
    /// If ticket was successfully queued, contains the ticket id
    /// and time to completion and expiration.
    /// </summary>
    public class MatchmakingTicketResponse : HttpResponse<MatchmakingTicketResponse>
    {
        public string ticketId { get; set; }
        public int expectedQueueTime { get; set; }
        public long lifetime { get; set; }
    }

    /// <summary>
    /// Request the current status of the given ticket.
    /// </summary>
    public class TicketStatusRequest : HttpRequest<TicketStatusRequest>
    {
        public string ticketId { get; set; }
    }

    /// <summary>
    /// Contains the current status of the requested ticket in the 
    /// response code. If ticket is complete, contains connection
    /// info for server.
    /// </summary>
    public class TicketStatusResponse : HttpResponse<TicketStatusResponse>
    {
        public string serverAddr { get; set; }
        public int tcpPort { get; set; }
        public int udpPort { get; set; }
        public SimulationSystem simulationSystem { get; set; }
        public int tickRate { get; set; }
    }
}