# OwlTree.Relays

Provides multiple types of relay servers to test your game in a networked environment. All are run with the .NET runtime.

You can read more about the OwlTree framework [here](https://github.com/CaptainToTo/owl-tree/wiki).

## SimpleRelayServer

This is a single relay server that useful for simple local testing. It allows host migration, and won't shutdown when empty allowing you to leave it running as you make changes to your game, and won't require constantly restarting it for each test.

Run with:
```
> dotnet run [AppId] [SessionId] [TCP] [UDP] [ThreadDelta] [SimulationSystem: m/l/r/s]
```

All arguments are optional, defaults are:
- app id: "MyOwlTreeApp"
- session id: "MyAppSession"
- tcp port: 8000
- udp port: 9000
- ThreadDelta: 20
- SimulationSystem: m (None / Message Queue)

Simulation System Options:
- `m`: None (use simple message queue)
- `l`: Lockstep
- `r`: Rollback
- `s`: Snapshot

The server will immediate start accepting clients. You can control the server using CLI commands:

```
Relay Server Commands:
  (h)elp:    prints this command list
  (p)layers: prints a list of players currently on the server
  (q)uit:    shutdown the relay server
  ports:     display the TCP and UDP ports the server is using
  ping [client id]: ping a client to test their latency
  (d)isconnect [client id]: disconnect a client from the server
```

The program will assign itself a random number id which will be associated with a log file it will output to.

## Monolith Relay Service

A monolith relay solution that provides session finding through an HTTP API, and relay management. This service is a single program. It is intended for testing, and low player counts.

Run with:
```
> dotnet run [IP] [API IP] [API Port]
```

The `IP` argument is the public IP that will given to clients to connect to relays. For local testing, use `127.0.0.1`. The API IP and port will be used for the endpoint to session finder API will listen on.

The server will immediately start processing matchmaking requests, and creating relay connections accordingly. You can control the server using CLI commands:

```
Relay server commands:
  (h)elp:    prints this command list
  (r)elays:  prints a list of relay servers currently running
  (q)uit:    shutdown the relay server
  (p)layers [app id] [session id]: prints a list of players currently on the server
  ping [app id] [session id] [client id]: ping a client to test their latency
  (d)isconnect [app id] [session id]: shutdown a session, disconnecting all of its clients
  (d)isconnect [app id] [session id] [client id]: disconnect a client from the server
```

Each relay created will have a file created for logging named by its app and session id.

Clients can make requests to the relay service using a `MatchmakingClient`, located in the Matchmaking project.

A host can request a new session by:

```cs
var requestClient = new MatchmakingClient("http://127.0.0.1:5000"); // the URL used by the server

var response = await requestClient.CreateSession(new SessionCreationRequest{
    appId = "MyOwlTreeApp",
    sessionId = "MyOwlTreeAppSession",
    maxClients = 10,
    migratable = false,
    simulationSystem = SimulationSystem.Snapshot,
    tickRate = 20
});

if (response.RequestFailed)
    Environment.Exit(0);

var connection = new Connection(new Connection.Args{
    role = NetRole.Host,
    serverAddr = response.serverAddr, // this will be the server ip given in the relay program args
    tcpPort = response.tcpPort,
    udpPort = response.udpPort,
    appId = "MyOwlTreeApp",
    sessionId = "MyOwlTreeAppSession"
});
```

And a client can request session connection args by:

```cs
var requestClient = new MatchmakingClient("http://127.0.0.1:5000"); // the URL used by the server

var response = await requestClient.CreateSession(new SessionDataRequest{
    appId = "MyOwlTreeApp",
    sessionId = "MyOwlTreeAppSession"
});

if (response.RequestFailed)
    Environment.Exit(0);

var connection = new Connection(new Connection.Args{
    role = NetRole.Client,
    serverAddr = response.serverAddr, // this will be the server ip given in the relay program args
    tcpPort = response.tcpPort,
    udpPort = response.udpPort,
    appId = "MyOwlTreeApp",
    sessionId = "MyOwlTreeAppSession",
    maxClients = response.maxClients,
    migratable = response.migratable,
    simulationSystem = response.simulationSystem,
    simulationTickRate = response.tickRate
});
```

## Monolith Session Finder

A monolith peer-to-peer session service. Used to publish hosted sessions, and for clients to request
host connection data. Currently not operable, since OwlTree doesn't support NAT punch-through yet.

## Test Client

Use to test if you've correctly set up your relay server. Each client will assign itself a random number
which it will use to output logs, and hosts will use it for their session id.

To test the simple relay server:

```
> dotnet run [AppId] [SessionId] [IP Address] [TCP] [UDP]
```

To test the monolith relay service:

```
> dotnet run [AppId] [API Endpoint] [client/host] [SessionId]
> dotnet run MyAppId http://my-endpoint.com host
> dotnet run MyAppId http://my-endpoint.com client 123
```

Here, 123 is the random id the host assigned itself.

## OwlTree.Matchmaking

This is a matchmaking library you can use for passing connection args to clients. The library contains a
`MatchmakingClient` for making requests, and a `MatchmakingEndpoint` for handle requests and sending responses.