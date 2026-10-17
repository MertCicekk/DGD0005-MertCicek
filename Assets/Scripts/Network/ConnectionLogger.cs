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
    }

    private void OnDestroy()
    {
        if (_nm == null) return;
        _nm.ServerManager.OnServerConnectionState -= OnServerConnectionState;
        _nm.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        _nm.ClientManager.OnClientConnectionState -= OnClientConnectionState;
    }

    // this process as a server: Starting -> Started -> Stopping -> Stopped
    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
        Debug.Log($"[Server] server {args.ConnectionState}");
    }

    // another client, as the server sees it
    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        string what = args.ConnectionState == RemoteConnectionState.Started ? "connected" : "disconnected";
        Debug.Log($"[Server] client {conn.ClientId} {what}. Clients: {_nm.ServerManager.Clients.Count}");
    }

    // this process as a client
    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        Debug.Log($"[Client] client {args.ConnectionState}");
    }
}
