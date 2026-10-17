// Pong — the given bootstrap (Tools/). Put it on the NetworkManager object. Do not edit.
// Starts the SERVER, and only the server, when this process is meant to be one:
//   - a headless / batch-mode build (the Dedicated Server build, next week), or
//   - the command line contains -server, or
//   - a Multiplayer Play Mode instance tagged "Server" (Step 5: Player 2).
// That is why ServerManager > Start On Headless is unticked: this script decides, and it logs it.
using System;
using FishNet;
using Unity.Multiplayer.PlayMode;
using UnityEngine;

public class ServerBootstrap : MonoBehaviour
{
    [SerializeField] private string playModeTag = "Server";

    private void Start()
    {
        string reason = null;
        if (Application.isBatchMode) reason = "batch mode";
        else if (HasArg("-server")) reason = "-server argument";
        else if (HasPlayModeTag(playModeTag)) reason = $"Play Mode tag '{playModeTag}'";

        if (reason == null) return;

        Debug.Log($"[Server] ServerBootstrap: starting the server ({reason}).");
        InstanceFinder.NetworkManager.ServerManager.StartConnection();
    }

    private static bool HasArg(string arg)
    {
        foreach (string a in Environment.GetCommandLineArgs())
            if (string.Equals(a, arg, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static bool HasPlayModeTag(string tag)
    {
        foreach (string t in CurrentPlayer.Tags)
            if (string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
