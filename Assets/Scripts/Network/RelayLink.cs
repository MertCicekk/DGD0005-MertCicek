// Homework 3, Step 5. Unity Relay for the host and the guest. No IP address: the host asks the Relay
// for a slot and gets a join code; the guest gives the code and gets the way to the same Relay server.
// Both then talk only to the Relay. ConnectionHud calls it (Step 6).
using System.Threading.Tasks;
using FishNet.Managing;
using FishNet.Transporting.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public static class RelayLink
{
    private const int MaxGuests = 1;                    // Pong: the host + one guest
    private const string ConnectionType = "dtls";       // encrypted UDP to the Relay

    /// Unity Services must be started and the player signed in before any Relay call.
    private static async Task SignIn()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private static UnityTransport Transport(NetworkManager nm)
    {
        UnityTransport t = nm.TransportManager.GetTransport<UnityTransport>();
        if (t == null) Debug.LogError("[Relay] No UnityTransport on the NetworkManager object.");
        return t;
    }

    /// Host: reserve a Relay slot, get its join code, start server + client through the Relay.
    /// Returns the join code.
    public static async Task<string> StartHost(NetworkManager nm)
    {
        await SignIn();

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxGuests);
        string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        Debug.Log($"[Relay] Join code: {code}");

        UnityTransport transport = Transport(nm);
        if (transport == null) return null;
        transport.SetRelayServerData(allocation.ToRelayServerData(ConnectionType));   // BEFORE StartConnection

        nm.ServerManager.StartConnection();
        nm.ClientManager.StartConnection();
        return code;
    }

    /// Guest: turn the join code into the host's Relay data, start the client through the Relay.
    public static async Task StartGuest(NetworkManager nm, string code)
    {
        await SignIn();

        JoinAllocation join = await RelayService.Instance.JoinAllocationAsync(code.Trim().ToUpperInvariant());

        UnityTransport transport = Transport(nm);
        if (transport == null) return;
        transport.SetRelayServerData(join.ToRelayServerData(ConnectionType));         // BEFORE StartConnection

        nm.ClientManager.StartConnection();
    }
}
