using OwlTree.Matchmaking;

public static class HostReportUris
{
    public const string HostConnected = "/host-connected";
    public const string ClientCount = "/client-count";
    public const string Shutdown = "/shutdown";
    public const string HostPing = "/ping";
}

public class HostConnectedReport : HttpRequest<HostConnectedReport>
{
    public string appId { get; set; }
    public string sessionId { get; set; }
}

public class ClientCountReport : HttpRequest<ClientCountReport>
{
    public string appId { get; set; }
    public string sessionId { get; set; }
    public int clientCount { get; set; }
}

public class SessionShutdownReport : HttpRequest<SessionShutdownReport>
{
    public string appId { get; set; }
    public string sessionId { get; set; }
}

public class HostPingRequest : HttpRequest<HostPingRequest>
{
    public string appId { get; set; }
    public string sessionId { get; set; }
    public long timestamp { get; set; }
}

public class HostPingResponse : HttpResponse<HostPingResponse>
{
    public long timestamp { get; set; }
}