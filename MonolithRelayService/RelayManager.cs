
using System.Collections.Concurrent;
using OwlTree;

/// <summary>
/// Creates, manages, and shutdown relays.
/// </summary>
public class RelayManager
{
    private ConcurrentDictionary<string, Connection> _connections = new();
    private ConcurrentDictionary<string, long> _startTimes = new();

    /// <summary>
    /// The maximum number of relays this manager can have active at once.
    /// </summary>
    public int Capacity { get; private set; }

    /// <summary>
    /// Returns true if the relay manager is at capacity.
    /// </summary>
    public bool IsFull => Capacity == Count;

    /// <summary>
    /// the number of relays currently active.
    /// </summary>
    public int Count => _connections.Count;

    /// <summary>
    /// Iterable of all currently active relays.
    /// </summary>
    public IEnumerable<Connection> Connections => _connections.Values;

    /// <summary>
    /// Create a new relay manager.
    /// </summary>
    /// <param name="capacity">The max number of relays</param>
    /// <param name="threadUpdateDelta">The frequency of queue executions, in milliseconds</param>
    /// <param name="timeout">The duration a relay can remain active without any clients, in milliseconds</param>
    public RelayManager(int capacity = -1, int threadUpdateDelta = 40, long timeout = 60000)
    {
        Capacity = capacity == -1 ? int.MaxValue : capacity;
        _threadUpdateDelta = threadUpdateDelta;
        _timeout = timeout;
        IsActive = true;
        _thread = new Thread(ThreadLoop);
        _thread.Start();
    }

    private long _timeout;

    private Thread _thread;
    private int _threadUpdateDelta;

    /// <summary>
    /// True if this manager is currently able to manage relays.
    /// False if this manager has been shutdown, in which case a new manager will need to be created
    /// to continue managing relays.
    /// </summary>
    public bool IsActive { get; private set; }

    private void ThreadLoop()
    {
        List<string> toBeRemoved = new();
        while (IsActive)
        {
            long start = Timestamp.Now;
            foreach (var pair in _connections)
            {
                pair.Value.ExecuteQueue();
                if (
                    !pair.Value.IsActive || 
                    (
                        // shutdown a relay if it has been empty for too long
                        pair.Value.ClientCount == 0 && 
                        _startTimes.TryGetValue(pair.Key, out var startTime) && 
                        start - startTime > _timeout
                    )
                )
                {
                    toBeRemoved.Add(pair.Key);
                }
            }
            // shutdown relays marked for removal
            foreach (var sessionId in toBeRemoved)
            {
                if (_connections[sessionId].IsActive)
                    _connections[sessionId].Disconnect();
                _connections.Remove(sessionId, out var connection);
                _startTimes.Remove(sessionId, out var time);
            }
            toBeRemoved.Clear();
            long diff = Timestamp.Now - start;

            Thread.Sleep(Math.Max(0, _threadUpdateDelta - (int)diff));
        }

        // once the update loop has been exited, shutdown the manager

        IsActive = false;
        foreach (var connection in Connections)
            connection.Disconnect();
        
        _connections.Clear();
        _startTimes.Clear();
    }

    public static string GetKey(Connection.Args args) => args.appId + "+" + args.sessionId;
    public static string GetKey(Connection relay) => relay.AppId + "+" + relay.SessionId;
    public static string GetKey(string appId, string sessionId) => appId + "+" + sessionId;

    /// <summary>
    /// Create a new relay connection with the given args.
    /// Returns the created relay.
    /// </summary>
    public Connection Add(Connection.Args args)
    {
        if (_connections.Count >= Capacity)
            throw new InvalidOperationException("Cannot create more connections, manager is at capacity.");

        if (_connections.ContainsKey(GetKey(args)))
            throw new ArgumentException($"'{args.sessionId}' already exists.");

        var connection = new Connection(args);

        if (!_connections.TryAdd(GetKey(args), connection))
        {
            connection.Disconnect();
            throw new InvalidOperationException("Failed to cache new connection.");
        }
        _startTimes.TryAdd(args.sessionId, Timestamp.Now);

        return connection;
    }

    /// <summary>
    /// Gets a relay using the given app and session id. Returns null if no such relay is found.
    /// </summary>
    public Connection Get(string appId, string sessionId)
    {
        return _connections.GetValueOrDefault(GetKey(appId, sessionId));
    }

    /// <summary>
    /// Tries to get a relay using the given app and session id. Returns false if no such relay is found.
    /// </summary>
    public bool TryGet(string appId, string sessionId, out Connection relay)
    {
        return _connections.TryGetValue(GetKey(appId, sessionId), out relay);
    }

    /// <summary>
    /// Returns true if the given session id exists among the relays currently active.
    /// </summary>
    public bool Contains(string appId, string sessionId) => _connections.ContainsKey(GetKey(appId, sessionId));
    
    /// <summary>
    /// Shutdown the manager, and all relays it manages.
    /// </summary>
    public void DisconnectAll()
    {
        IsActive = false;
    }
}