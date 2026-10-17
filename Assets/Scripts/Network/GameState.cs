// Pong — Steps 7 and 9. The score everybody shares. On an empty "GameState" scene object
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

    /// The last six events. A container as state: late joiners get all six at once.
    public readonly SyncList<string> Feed = new SyncList<string>();

    private const int FeedLength = 6;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ConnectionHud.Panels.Add(() => IsSpawned
            ? $"<b>LEFT {LeftScore.Value} : {RightScore.Value} RIGHT</b>\nServe: {PaddleController.SideName(ServingSide.Value)}"
            : null);
        ConnectionHud.Panels.Add(FeedText);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private string FeedText()
    {
        if (!IsSpawned || Feed.Count == 0) return null;
        return "<b>Feed</b>\n" + string.Join("\n", Feed);
    }

    /// Server only. Called by the ball when it crosses a goal line.
    public void Goal(int scorerSide)
    {
        if (!IsServerInitialized) return;

        // 1. state first: SyncVar writes
        if (scorerSide == PaddleController.Left) LeftScore.Value++;
        else                                     RightScore.Value++;
        ServingSide.Value = 1 - scorerSide;      // the side that conceded serves

        Announce($"{PaddleController.SideName(scorerSide)} scored. {LeftScore.Value} : {RightScore.Value}");

        // 2. then the events: everyone hears the goal, only the conceder hears YOU SERVE
        PaddleController scorer   = PaddleController.FindBySide(scorerSide);
        PaddleController conceder = PaddleController.FindBySide(1 - scorerSide);
        if (scorer != null)   scorer.AnnounceGoal(LeftScore.Value, RightScore.Value);
        if (conceder != null) conceder.YouServe(conceder.Owner);
    }

    /// Server only. Adds a line to the feed; keeps the last six.
    public static void Announce(string line)
    {
        if (Instance == null || !Instance.IsServerInitialized) return;
        Instance.Feed.Add(line);
        while (Instance.Feed.Count > FeedLength) Instance.Feed.RemoveAt(0);
        Debug.Log($"[Server] feed: {line}");
    }
}
