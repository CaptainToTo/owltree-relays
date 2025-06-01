
using System.Net;
using System.Text;
using OwlTree;
using OwlTree.Matchmaking;

public class SessionData
{
    public readonly string appId;
    public readonly string sessionId;

    public readonly string ip;
    public readonly int tcpPort;
    public readonly int udpPort;
    public readonly int maxClients;
    public readonly SimulationSystem simulationSystem;
    public readonly int tickRate;
    public readonly long startTime;

    private int _clientCount;
    private long _lastContact;
    private bool _hostConnected;

    private readonly object _lock = new();

    public int ClientCount
    {
        get
        {
            lock (_lock)
                return _clientCount;
        }
        set
        {
            lock (_lock)
                _clientCount = value;
        }
    }

    public long LastContact
    {
        get
        {
            lock (_lock)
                return _lastContact;
        }
        set
        {
            lock (_lock)
                _lastContact = value;
        }
    }

    public bool HostConnected
    {
        get
        {
            lock (_lock)
                return _hostConnected;
        }
        set
        {
            lock (_lock)
                _hostConnected = value;
        }
    }

    public bool IsFull => ClientCount == maxClients;

    public SessionData(
        string appId, string sessionId,
        string ip, int tcpPort, int udpPort,
        int maxClients, SimulationSystem simulationSystem, int tickRate
    )
    {
        this.appId = appId;
        this.sessionId = sessionId;
        this.ip = ip;
        this.tcpPort = tcpPort;
        this.udpPort = udpPort;
        this.maxClients = maxClients;
        this.simulationSystem = simulationSystem;
        this.tickRate = tickRate;
        startTime = Timestamp.Now;
        _clientCount = 0;
        _lastContact = Timestamp.Now;
        _hostConnected = false;
    }
}

public class SessionStore
{
    private Dictionary<string, SessionData> _sessions = new();

    public int Count => _sessions.Count;

    public IEnumerable<SessionData> Sessions => _sessions.Values;

    public static string GetKey(string appId, string sessionId) => appId + "+" + sessionId;

    public readonly string reportingDomain;
    private HttpListener _endpoint;
    private Thread _thread;

    public bool IsActive { get; private set; } = true;

    private int _clearRate;
    private int _dropThreshold;

    public SessionStore(string reportDomain, int clearRate, int dropThreshold)
    {
        _clearRate = clearRate;
        _dropThreshold = dropThreshold;

        _endpoint = new HttpListener();
        reportingDomain = reportDomain;
        _endpoint.Prefixes.Add(reportDomain);
        _thread = new Thread(ThreadLoop);
        _thread.Start();
        HandleHostReports();
    }

    public async void HandleHostReports()
    {
        _endpoint.Start();

        while (IsActive)
        {
            var context = await _endpoint.GetContextAsync();
            var request = context.Request;
            var response = context.Response;

            try
            {
                var uri = request.Url?.AbsolutePath ?? "/";

                switch (uri)
                {
                    case HostReportUris.HostConnected:
                        response.StatusCode = (int)HandleHostConnected(request);
                        break;
                    case HostReportUris.ClientCount:
                        response.StatusCode = (int)HandleClientCount(request);
                        break;
                    case HostReportUris.Shutdown:
                        response.StatusCode = (int)HandleShutdown(request);
                        break;
                    case HostReportUris.HostPing:
                        response.StatusCode = (int)HandlePing(request);
                        break;
                    default:
                        response.StatusCode = (int)ResponseCodes.NotFound;
                        break;

                }
            }
            catch
            {
                response.StatusCode = (int)ResponseCodes.RequestRejected;
            }
            response.OutputStream.Close();
        }

        _endpoint.Close();
    }

    private ResponseCodes HandleHostConnected(HttpListenerRequest request)
    {
        var source = GetSource(request);
        string requestBody = new StreamReader(request.InputStream, Encoding.UTF8).ReadToEnd();
        var requestObj = HostConnectedReport.Deserialize(requestBody);

        var key = GetKey(requestObj.appId, requestObj.sessionId);

        if (!_sessions.TryGetValue(key, out var data))
            return ResponseCodes.RequestRejected;

        if (data.ip != source.ToString())
            return ResponseCodes.RequestRejected;

        data.HostConnected = true;
        return ResponseCodes.RequestAccepted;
    }

    private ResponseCodes HandleClientCount(HttpListenerRequest request)
    {
        var source = GetSource(request);
        string requestBody = new StreamReader(request.InputStream, Encoding.UTF8).ReadToEnd();
        var requestObj = ClientCountReport.Deserialize(requestBody);

        var key = GetKey(requestObj.appId, requestObj.sessionId);

        if (!_sessions.TryGetValue(key, out var data))
            return ResponseCodes.RequestRejected;

        if (data.ip != source.ToString())
            return ResponseCodes.RequestRejected;

        data.ClientCount = requestObj.clientCount;
        return ResponseCodes.RequestAccepted;
    }

    private ResponseCodes HandleShutdown(HttpListenerRequest request)
    {
        var source = GetSource(request);
        string requestBody = new StreamReader(request.InputStream, Encoding.UTF8).ReadToEnd();
        var requestObj = SessionShutdownReport.Deserialize(requestBody);

        var key = GetKey(requestObj.appId, requestObj.sessionId);

        if (!_sessions.TryGetValue(key, out var data))
            return ResponseCodes.RequestRejected;

        if (data.ip != source.ToString())
            return ResponseCodes.RequestRejected;

        data.HostConnected = false;
        return ResponseCodes.RequestAccepted;
    }

    private ResponseCodes HandlePing(HttpListenerRequest request)
    {
        var source = GetSource(request);
        string requestBody = new StreamReader(request.InputStream, Encoding.UTF8).ReadToEnd();
        var requestObj = HostPingRequest.Deserialize(requestBody);

        var key = GetKey(requestObj.appId, requestObj.sessionId);

        if (!_sessions.TryGetValue(key, out var data))
            return ResponseCodes.RequestRejected;

        if (data.ip != source.ToString())
            return ResponseCodes.RequestRejected;

        data.LastContact = Timestamp.Now;
        return ResponseCodes.RequestAccepted;
    }

    private void ThreadLoop()
    {
        List<string> toBeRemoved = new();
        while (IsActive)
        {
            long start = Timestamp.Now;
            foreach (var pair in _sessions)
            {
                if (pair.Value.HostConnected && start - pair.Value.LastContact > _dropThreshold)
                    toBeRemoved.Add(pair.Key);
                else if (!pair.Value.HostConnected && start - pair.Value.startTime > _dropThreshold)
                    toBeRemoved.Add(pair.Key);
            }

            foreach (var key in toBeRemoved)
                _sessions.Remove(key);
            toBeRemoved.Clear();
            long diff = Timestamp.Now - start;

            Thread.Sleep(Math.Max(0, _clearRate - (int)diff));
        }

        _sessions.Clear();
    }

    public bool Add(SessionData data)
    {
        if (_sessions.ContainsKey(GetKey(data.appId, data.sessionId)))
            return false;

        _sessions.Add(GetKey(data.appId, data.sessionId), data);
        return true;
    }

    public void Remove(string appId, string sessionId)
    {
        if (_sessions.ContainsKey(GetKey(appId, sessionId)))
            _sessions[GetKey(appId, sessionId)].HostConnected = false;
    }

    public bool TryGet(string appId, string sessionId, out SessionData data)
    {
        if (_sessions.ContainsKey(GetKey(appId, sessionId)))
        {
            data = null;
            return false;
        }

        data = _sessions[GetKey(appId, sessionId)];
        return true;
    }

    public bool Contains(string appId, string sessionId)
    {
        return _sessions.ContainsKey(GetKey(appId, sessionId));
    }

    private IPAddress GetSource(HttpListenerRequest request)
    {
        return request.Headers["X-Real-IP"] != null ? IPAddress.Parse(request.Headers["X-Real-IP"]) : request.RemoteEndPoint.Address;
    }

    public void Stop()
    {
        IsActive = false;
    }
}