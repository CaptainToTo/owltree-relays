
using OwlTree;

public static class Commands
{
    public static void Help()
    {
        Console.WriteLine("Relay server commands:");
        Console.WriteLine("  (h)elp:    prints this command list");
        Console.WriteLine("  (s)essions:  prints a list of sessions currently running");
        Console.WriteLine("  (q)uit:    shutdown the session finder");
        Console.WriteLine("  (r)remove [app id] [session id]: removes a session from the list");
    }

    public static void Remove(SessionStore sessions, string appId, string sessionId)
    {
        if (!sessions.Contains(appId, sessionId))
        {
            Console.WriteLine("session does not exist");
            return;
        }

        sessions.Remove(appId, sessionId);
        Console.WriteLine("removing session...");
    }

    internal static void SessionList(SessionStore sessions)
    {
        if (sessions == null) return;

        if (sessions.Count == 0)
        {
            Console.WriteLine("No active relays");
            return;
        }

        Console.WriteLine("Relays:");
        foreach (var session in sessions.Sessions)
            Console.WriteLine($"   {session.appId} - {session.sessionId}: IP: {session.ip}, TCP: {session.tcpPort}, UDP: {session.udpPort}, {session.ClientCount}/{session.maxClients} clients");
    }
}