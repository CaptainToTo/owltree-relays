
using OwlTree;

public static class Commands
{
    public static void Help()
    {
        Console.WriteLine("Relay server commands:");
        Console.WriteLine("  (h)elp:    prints this command list");
        Console.WriteLine("  (r)elays:  prints a list of relay servers currently running");
        Console.WriteLine("  (q)uit:    shutdown the relay server");
        Console.WriteLine("  (p)layers [app id] [session id]: prints a list of players currently on the server");
        Console.WriteLine("  ping [app id] [session id] [client id]: ping a client to test their latency");
        Console.WriteLine("  (d)isconnect [app id] [session id]: shutdown a session, disconnecting all of its clients");
        Console.WriteLine("  (d)isconnect [app id] [session id] [client id]: disconnect a client from the server");
    }

    public static void PlayerList(Connection relay)
    {
        if (relay.ClientCount == 0)
        {
            Console.WriteLine("no players connected");
            return;
        }

        Console.WriteLine("players:");
        foreach (var player in relay.Clients)
        {
            Console.WriteLine($"  {player} {(player == relay.Authority ? "[host]" : "")}");
        }
    }

    public static void Ping(string id, Connection relay)
    {
        if (!uint.TryParse(id, out var result))
        {
            Console.WriteLine("  ping failed...");
            return;
        }
        var clientId = new ClientId(result);
        if (!relay.ContainsClient(clientId))
        {
            Console.WriteLine("  ping failed...");
            return;
        }

        var ping = relay.Ping(clientId);

        Console.WriteLine($"  pinging {clientId}...");

        while (!ping.Resolved)
        {
            relay.ExecuteQueue();
            Thread.Sleep(10);
        }

        if (ping.Failed)
        {
            Console.WriteLine("  ping failed...");
        }
        else
        {
            Console.WriteLine($"  {clientId} ping: {ping.Ping} ms");
        }
    }

    public static void Disconnect(Connection relay)
    {
        relay.Disconnect();
        Console.WriteLine("shutting down relay...");
    }

    public static void Disconnect(string id, Connection relay)
    {
        if (!uint.TryParse(id, out var result))
        {
            Console.WriteLine("  disconnect failed...");
            return;
        }
        var clientId = new ClientId(result);
        if (!relay.ContainsClient(clientId))
        {
            Console.WriteLine("  disconnect failed...");
            return;
        }

        relay.Disconnect(clientId);
        Console.WriteLine("disconnecting " + clientId);
    }

    internal static void RelayList(RelayManager relays)
    {
        if (relays == null) return;

        if (relays.Count == 0)
        {
            Console.WriteLine("No active relays");
            return;
        }

        Console.WriteLine("Relays:");
        foreach (var relay in relays.Connections)
            Console.WriteLine($"   {relay.AppId.Id} - {relay.SessionId.Id}: TCP: {relay.ServerTcpPort}, UDP: {relay.ServerUdpPort}, {relay.ClientCount}/{relay.MaxClients} clients");
    }
}