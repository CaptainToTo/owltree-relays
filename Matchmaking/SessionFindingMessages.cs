using OwlTree;

namespace OwlTree.Matchmaking
{
    public static partial class Uris
    {
        public const string CreateSession = "/create-session";
        public const string PublishSession = "/publish-session";
        public const string SessionData = "/get-session";
    }


    /// <summary>
    /// Sent by a host in a relayed peer-to-peer architecture to 
    /// have a new relay server be made for session they will host.
    /// </summary>
    public class SessionCreationRequest : HttpRequest<SessionCreationRequest>
    {
        public string appId { get; set; }
        public string sessionId { get; set; }
        public int maxClients { get; set; }
        public bool migratable { get; set; }
        public SimulationSystem simulationSystem { get; set; }
        public int tickRate { get; set; }
    }

    /// <summary>
    /// If the host's request was successful, will contain the endpoint
    /// info for the relay server.
    /// </summary>
    public class SessionCreationResponse : HttpResponse<SessionCreationResponse>
    {
        public string serverAddr { get; set; }
        public int tcpPort { get; set; }
        public int udpPort { get; set; }
    }

    /// <summary>
    /// Sent by clients to request connection data for a given session
    /// from a given app.
    /// </summary>
    public class SessionDataRequest : HttpRequest<SessionDataRequest>
    {
        public string appId { get; set; }
        public string sessionId { get; set; }
    }

    /// <summary>
    /// If client's request was successful, will contain the connection
    /// info needed to connect the requested session.
    /// </summary>
    public class SessionDataResponse : HttpResponse<SessionDataResponse>
    {
        public string serverAddr { get; set; }
        public int tcpPort { get; set; }
        public int udpPort { get; set; }
        public int maxClients { get; set; }
        public bool migratable { get; set; }
        public SimulationSystem simulationSystem { get; set; }
        public int tickRate { get; set; }
    }

    /// <summary>
    /// Sent by a host in a peer-to-peer architecture to publish
    /// their session to the session finding service.
    /// </summary>
    public class SessionPublishRequest : HttpRequest<SessionPublishRequest>
    {
        public string appId { get; set; }
        public string sessionId { get; set; }
        public int maxPlayers { get; set; }
        public string hostAddr { get; set; } = "*";
        public int tcpPort { get; set; }
        public int udpPort { get; set; }
        public SimulationSystem simulationSystem { get; set; }
        public int tickRate { get; set; }
    }

    /// <summary>
    /// If host's publishing request was successful, will contain
    /// the endpoint the host was assign for reporting session events
    /// to the server.
    /// </summary>
    public class SessionPublishResponse : HttpResponse<SessionPublishResponse>
    {
        public string reportingEndpoint { get; set; }
    }
}