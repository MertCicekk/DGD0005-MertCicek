// Pong — Steps 7 and 8. The score everybody shares. On an empty "GameState" scene object
// with a NetworkObject: it belongs to nobody, the server writes it, every client mirrors it,
// and a client that joins late receives the current values on spawn.
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class GameState : NetworkBehaviour
{
    public static GameState Instance;

    public readonly SyncVar<int> LeftScore   = new SyncVar<int>();
    public readonly SyncVar<int> RightScore  = new SyncVar<int>();
    public readonly SyncVar<int> ServingSide = new SyncVar<int>(PaddleController.Left);

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ConnectionHud.Panels.Add(() => IsSpawned
            ? $"<b>LEFT {LeftScore.Value} : {RightScore.Value} RIGHT</b>\nServe: {PaddleController.SideName(ServingSide.Value)}"
            : null);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// Server only. Called by the ball when it crosses a goal line.
    public void Goal(int scorerSide)
    {
        if (!IsServerInitialized) return;

        // 1. state first: SyncVar writes
        if (scorerSide == PaddleController.Left) LeftScore.Value++;
        else                                     RightScore.Value++;
        ServingSide.Value = 1 - scorerSide;      // the side that conceded serves

        // 2. then the events: everyone hears the goal, only the conceder hears YOU SERVE
        PaddleController scorer   = PaddleController.FindBySide(scorerSide);
        PaddleController conceder = PaddleController.FindBySide(1 - scorerSide);
        if (scorer != null)   scorer.AnnounceGoal(LeftScore.Value, RightScore.Value);
        if (conceder != null) conceder.YouServe(conceder.Owner);
    }
}
