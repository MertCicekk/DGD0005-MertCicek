// Pong — the given HUD (Tools/). Put it on an empty "Hud" object. Do not edit.
// Start Server / Start Client buttons, the address field, RTT, tick, client count and bytes per second.
// Your scripts draw through it:
//   ConnectionHud.Panels.Add(() => "text");   a panel that is redrawn every frame (score, feed, ...)
//   ConnectionHud.Flash("LEFT scored!");       a big message for two seconds
using System;
using System.Collections.Generic;
using System.Reflection;
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Statistic;
using FishNet.Transporting.Tugboat;
using UnityEngine;

public class ConnectionHud : MonoBehaviour
{
    /// Text panels your scripts add. Each one is called every frame and drawn under the connection box.
    public static readonly List<Func<string>> Panels = new List<Func<string>>();

    private static string _flash;
    private static float _flashUntil;

    /// Shows a big message in the middle of the screen for two seconds, on THIS machine.
    public static void Flash(string message)
    {
        _flash = message;
        _flashUntil = Time.time + 2f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()             // works with "Enter Play Mode Options" too
    {
        Panels.Clear();
        _flash = null;
        _flashUntil = 0f;
    }

    private string _address = "localhost";
    private NetworkManager _nm;

    // bytes per second, measured from the StatisticsManager (Step 10)
    private NetworkTrafficStatistics _stats;
    private ulong _serverIn, _serverOut, _clientIn, _clientOut;
    private ulong _serverInPs, _serverOutPs, _clientInPs, _clientOutPs;
    private float _windowStart;
    private static FieldInfo _inField, _outField, _bytesField;

    private GUIStyle _box, _flashStyle;

    private void Start()
    {
        _nm = InstanceFinder.NetworkManager;
        if (_nm == null) { Debug.LogWarning("[Hud] No NetworkManager in the scene."); return; }

        Tugboat tugboat = _nm.TransportManager.GetTransport<Tugboat>();
        if (tugboat != null) _address = tugboat.GetClientAddress();

        StatisticsManager sm = _nm.StatisticsManager;
        if (sm != null && sm.TryGetNetworkTrafficStatistics(out _stats))
            _stats.OnNetworkTraffic += OnTraffic;
        _windowStart = Time.unscaledTime;
    }

    private void OnDestroy()
    {
        if (_stats != null) _stats.OnNetworkTraffic -= OnTraffic;
    }

    private void OnTraffic(uint tick, object serverTraffic, object clientTraffic)
    {
        _serverIn  += Bytes(serverTraffic, true);
        _serverOut += Bytes(serverTraffic, false);
        _clientIn  += Bytes(clientTraffic, true);
        _clientOut += Bytes(clientTraffic, false);
    }

    // FishNet keeps the byte counters internal; read them once by reflection.
    private static ulong Bytes(object bidirectional, bool inbound)
    {
        if (bidirectional == null) return 0;
        const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        if (_inField == null)
        {
            Type t = bidirectional.GetType();
            _inField  = t.GetField("InboundTraffic", F);
            _outField = t.GetField("OutboundTraffic", F);
        }
        object traffic = (inbound ? _inField : _outField)?.GetValue(bidirectional);
        if (traffic == null) return 0;
        if (_bytesField == null) _bytesField = traffic.GetType().GetField("Bytes", F);
        return _bytesField != null ? (ulong)_bytesField.GetValue(traffic) : 0;
    }

    private void Update()
    {
        if (Time.unscaledTime - _windowStart < 1f) return;
        _serverInPs = _serverIn; _serverOutPs = _serverOut; _clientInPs = _clientIn; _clientOutPs = _clientOut;
        _serverIn = _serverOut = _clientIn = _clientOut = 0;
        _windowStart = Time.unscaledTime;
    }

    private void OnGUI()
    {
        if (_box == null)
        {
            _box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 12, richText = true, padding = new RectOffset(6, 6, 4, 4) };
            _flashStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 40, fontStyle = FontStyle.Bold };
        }

        GUILayout.BeginArea(new Rect(8, 8, 250, Screen.height - 16));

        if (_nm == null)
        {
            GUILayout.Box("No NetworkManager in the scene.", _box);
            GUILayout.EndArea();
            return;
        }

        bool server = _nm.IsServerStarted;
        bool client = _nm.IsClientStarted;

        // --- buttons ---
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(server ? "Stop Server" : "Start Server", GUILayout.Height(24)))
        {
            if (server) _nm.ServerManager.StopConnection(true);
            else        _nm.ServerManager.StartConnection();
        }
        if (GUILayout.Button(client ? "Stop Client" : "Start Client", GUILayout.Height(24)))
        {
            if (client) _nm.ClientManager.StopConnection();
            else        _nm.ClientManager.StartConnection(_address.Trim());
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Address", GUILayout.Width(55));
        GUI.enabled = !client;
        _address = GUILayout.TextField(_address);
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        // --- connection box ---
        string role = server && client ? "Server + Client" : server ? "Server" : client ? "Client" : "Offline";
        string text = $"<b>{role}</b>";
        if (server) text += $"\nClients: {_nm.ServerManager.Clients.Count}";
        if (client) text += $"\nClientId: {_nm.ClientManager.Connection.ClientId}   RTT: {_nm.TimeManager.RoundTripTime} ms";
        if (server || client) text += $"\nTick: {_nm.TimeManager.Tick}   Tick rate: {_nm.TimeManager.TickRate}";
        if (_stats == null)
            text += "\n<color=#888>bytes/s: StatisticsManager off</color>";
        else
        {
            if (server) text += $"\nServer in/out B/s: {_serverInPs} / {_serverOutPs}";
            if (client) text += $"\nClient in/out B/s: {_clientInPs} / {_clientOutPs}";
        }
        GUILayout.Box(text, _box);
        GUILayout.EndArea();

        // --- your panels, top right ---
        GUILayout.BeginArea(new Rect(Screen.width - 238, 8, 230, Screen.height - 16));
        for (int i = 0; i < Panels.Count; i++)
        {
            string s;
            try { s = Panels[i]?.Invoke(); }
            catch (Exception) { s = null; }      // a panel whose object is gone
            if (!string.IsNullOrEmpty(s)) GUILayout.Box(s, _box);
        }

        GUILayout.EndArea();

        // --- flash ---
        if (_flash != null && Time.time < _flashUntil)
            GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 80), _flash, _flashStyle);
    }
}
