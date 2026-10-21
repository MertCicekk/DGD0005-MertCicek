// Pong — Steps 4 and 8. The networked paddle. One prefab, spawned once per connection by the PlayerSpawner.
// The same script runs on the server, on the owner's client and on every other client;
// only the answer to IsOwner differs (Notes 4.4, 5).
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

public class PaddleController : NetworkBehaviour
{
    public const int Left = 0, Right = 1, Spectator = 2;

    /// Every paddle this machine knows about (the server uses it for the ball in Step 7).
    public static readonly List<PaddleController> All = new List<PaddleController>();
    /// The paddle this client owns. Null on the server.
    public static PaddleController Local;

    /// 0 = LEFT, 1 = RIGHT, 2 = spectator. Only the server writes it.
    public readonly SyncVar<int> Side = new SyncVar<int>(Spectator);

    [SerializeField] private float speed = 8f;

    /// Homework 2, Step 2. Server-only memory: the last input the server accepted.
    private float _serverAxis;

    /// Homework 2, Step 4. Server-only. One timestamp per paddle = one per client (NOT static).
    private const double MinInputGap = 0.030;           // seconds
    private double _lastInputTime = -1;
    public int RejectedInputs;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { All.Clear(); Local = null; }

    private void Awake()
    {
        Side.OnChange += OnSideChange;      // subscribe in Awake, before the first value arrives
    }

    private void OnSideChange(int prev, int next, bool asServer) => Place();

    // ---------- server ----------
    public override void OnStartServer()
    {
        base.OnStartServer();
        if (!All.Contains(this)) All.Add(this);

        int side = Spectator;                               // first free side, else spectator
        if (!IsTaken(Left)) side = Left;
        else if (!IsTaken(Right)) side = Right;
        Side.Value = side;

        Debug.Log($"[Server] client {OwnerId} gets side {SideName(side)}");
        ConnectionHud.Panels.Add(() => $"Rejected inputs (paddle {OwnerId}): {RejectedInputs}");   // Homework 2, Step 4

        GameState.Announce($"client {OwnerId} joined as {SideName(side)}");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        All.Remove(this);
        GameState.Announce($"client {OwnerId} left ({SideName(Side.Value)})");
    }

    private bool IsTaken(int side)
    {
        foreach (PaddleController p in All)
            if (p != this && p.Side.Value == side) return true;
        return false;
    }

    // ---------- every client ----------
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!All.Contains(this)) All.Add(this);

        if (IsOwner)
        {
            Local = this;
            Debug.Log($"[Client] I am client {OwnerId}, side {SideName(Side.Value)}");
        }
        Place();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        All.Remove(this);
        if (Local == this) Local = null;
    }

    // ---------- the owner ----------
    private void Update()
    {
        if (!IsOwner) return;                               // the line that stops everyone else moving my paddle

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.spaceKey.wasPressedThisFrame) RequestServe(); // ask; the server decides (Step 8)

        // ---------- Homework 2, Step 5: the cheating client (debug keys, the server must survive them) ----------
        if (kb.cKey.wasPressedThisFrame)                    // C: spam, 20 inputs in one frame
            for (int i = 0; i < 20; i++) ServerSetAxis(1f);

        if (kb.xKey.wasPressedThisFrame)                    // X: speed hack
            ServerSetAxis(50f);

        if (kb.tKey.wasPressedThisFrame && NetworkBall.Instance != null)   // T: teleport, on the client, no RPC
            transform.position = NetworkBall.Instance.transform.position;
    }

    // ---------- Step 8: the three RPC directions ----------

    /// client -> server. A request the server may refuse.
    [ServerRpc]
    private void RequestServe()
    {
        GameState gs = GameState.Instance;
        NetworkBall ball = NetworkBall.Instance;
        if (gs == null || ball == null) return;
        if (Side.Value != gs.ServingSide.Value) return;     // not your serve
        if (ball.InPlay.Value) return;                       // already in play

        ball.Serve(1 - Side.Value);                          // towards the other side
        GameState.Announce($"{SideName(Side.Value)} served");
    }

    /// server -> every client. An event everyone reacts to.
    [ObserversRpc]
    public void AnnounceGoal(int left, int right)
    {
        ConnectionHud.Flash($"{SideName(Side.Value)} scored! {left} : {right}");
    }

    /// server -> one client. A private message.
    [TargetRpc]
    public void YouServe(NetworkConnection target)
    {
        ConnectionHud.Flash("YOU SERVE");
    }

    public static PaddleController FindBySide(int side)
    {
        foreach (PaddleController p in All)
            if (p != null && p.Side.Value == side) return p;
        return null;
    }

    /// Puts the paddle on its side and colours it. Called on start and whenever Side changes.
    private void Place()
    {
        int side = Side.Value;
        Renderer r = GetComponent<Renderer>();
        if (side == Spectator)
        {
            if (r != null) r.enabled = false;
            return;
        }

        Vector3 p = transform.position;
        p.x = side == Left ? -Court.PaddleX : Court.PaddleX;
        p.y = 0.5f;
        transform.position = p;

        if (r != null) r.enabled = true;
        Court.Paint(gameObject, side == Left ? new Color(0.35f, 0.65f, 1f) : new Color(1f, 0.60f, 0.25f));
    }

    public static string SideName(int side) => side == Left ? "LEFT" : side == Right ? "RIGHT" : "SPECTATOR";

    // ---------- Homework 2, Step 2: input up, position down ----------
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        TimeManager.OnTick += OnTick;                       // here, not in Awake: TimeManager exists only once networked
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        if (TimeManager != null) TimeManager.OnTick -= OnTick;
    }

    private void OnTick()
    {
        // 1. Owner sends: input, not position. Every tick, also when it is 0.
        if (IsOwner && Side.Value != Spectator)
        {
            Keyboard kb = Keyboard.current;
            float axis = 0f;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed)   axis += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) axis -= 1f;
            }
            ServerSetAxis(axis);
        }

        // 2. Server moves: the only place the paddle position is written.
        if (IsServerInitialized)
        {
            transform.position += Vector3.forward * _serverAxis * speed * (float)TimeManager.TickDelta;
            Court.ClampPaddle(transform);
        }
    }

    /// client -> server. The owner's input for this tick. The server checks it before it believes it:
    /// Step 3 checks (NaN, clamp, spectator) -> Step 4 rate limit -> write state.
    [ServerRpc]
    private void ServerSetAxis(float axis)
    {
        // Step 3: check the input
        if (float.IsNaN(axis)) return;
        axis = Mathf.Clamp(axis, -1f, 1f);                  // a client that sends 50 gets 1
        if (Side.Value == Spectator) return;                // a spectator has no paddle to move

        // Step 4: no second input inside 30 ms
        double now = TimeManager.TicksToTime(TimeManager.Tick);
        if (_lastInputTime >= 0 && now - _lastInputTime < MinInputGap)
        {
            RejectedInputs++;
            Debug.Log($"[Server] Rejected input from client {OwnerId}: too fast.");
            return;
        }
        _lastInputTime = now;

        _serverAxis = axis;
    }
}
