// Pong — Step 3. Prints every connection event, so the Console tells the story of a connection.
// A plain MonoBehaviour on the NetworkManager object. Filter the Console with [Server] or [Client].
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;

public class ConnectionLogger : MonoBehaviour
{
    private NetworkManager _nm;

    // Start, not Awake: in Awake the NetworkManager may not be ready yet (Notes 4.1, 5).
    private void Start()
    {
        _nm = InstanceFinder.NetworkManager;
        if (_nm == null) { Debug.LogWarning("[ConnectionLogger] No NetworkManager found."); return; }

        _nm.ServerManager.OnServerConnectionState += OnServerConnectionState;
        _nm.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        _nm.ClientManager.OnClientConnectionState += OnClientConnectionState;
        _nm.ClientManager.OnAuthenticated += OnAuthenticated;
    }

    private void OnDestroy()
    {
        if (_nm == null) return;
        _nm.ServerManager.OnServerConnectionState -= OnServerConnectionState;
        _nm.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        _nm.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        _nm.ClientManager.OnAuthenticated -= OnAuthenticated;
    }

    // this process as a server: Starting -> Started -> Stopping -> Stopped
    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
        Debug.Log($"[Server] {args.ConnectionState}");
    }

    // another client, as the server sees it
    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        string what = args.ConnectionState == RemoteConnectionState.Started ? "connected" : "disconnected";
        Debug.Log($"[Server] Client {conn.ClientId} {what}");
    }

    // this process as a client
    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        Debug.Log($"[Client] {args.ConnectionState}");

        // Week 5, Step 2: checked here, in the client handler, because the server is already started by now.
        if (args.ConnectionState == LocalConnectionState.Started && _nm.ServerManager.Started)
            Debug.Log("[Host] This process is the server AND a client. RTT is 0 ms.");
    }

    // the server has accepted this client and told it its id
    private void OnAuthenticated()
    {
        Debug.Log($"[Client] Connected, the server calls me client {_nm.ClientManager.Connection.ClientId}");
    }
}
