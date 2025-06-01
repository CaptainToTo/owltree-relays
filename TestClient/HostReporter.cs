using System.Text;
using OwlTree;
using OwlTree.Matchmaking;

public class HostReporter
{
    private string _endpoint;
    private HttpClient _client;

    private string _appId;
    private string _sessionId;

    private long _lastPing;
    private int _pingRate;

    public bool TimeForPing => Timestamp.Now - _lastPing > _pingRate;

    public bool Connected { get; private set; } = false;

    public HostReporter(string endpoint, string appId, string sessionId, int pingRate)
    {
        _endpoint = endpoint;
        _client = new HttpClient();
        _appId = appId;
        _sessionId = sessionId;
        _pingRate = pingRate;
    }

    public async Task<bool> Connect()
    {
        try
        {
            var request = new HostConnectedReport
            {
                appId = _appId,
                sessionId = _sessionId
            }.Serialize();
            var content = new StringContent(request, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync(_endpoint + Uris.HostConnected, content);

            Connected = response.IsSuccessStatusCode;

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async void ReportClientCount(int clientCount)
    {
        try
        {
            var request = new ClientCountReport
            {
                appId = _appId,
                sessionId = _sessionId,
                clientCount = clientCount
            }.Serialize();
            var content = new StringContent(request, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync(_endpoint + Uris.ClientCount, content);
        }
        catch
        {
        }
    }

    public async void ReportShutdown()
    {
        try
        {
            var request = new SessionShutdownReport
            {
                appId = _appId,
                sessionId = _sessionId
            }.Serialize();
            var content = new StringContent(request, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync(_endpoint + Uris.Shutdown, content);
        }
        catch
        {
        }
    }

    public async void Ping()
    {
        try
        {
            var request = new HostPingRequest
            {
                appId = _appId,
                sessionId = _sessionId,
                timestamp = Timestamp.Now
            }.Serialize();
            var content = new StringContent(request, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync(_endpoint + Uris.HostPing, content);

            if (response.IsSuccessStatusCode)
            {
                _lastPing = Timestamp.Now;
            }
        }
        catch
        {
        }
    }
}