// Pong — Step 4. The networked paddle. One prefab, spawned once per connection by the PlayerSpawner.
// The same script runs on the server, on the owner's client and on every other client;
// only the answer to IsOwner differs (Notes 4.4, 5).
using System.Collections.Generic;
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
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        All.Remove(this);
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
        if (Side.Value == Spectator) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        float dir = 0f;                                     // one keyboard per machine: W/S or arrows
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)   dir += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir -= 1f;

        transform.position += Vector3.forward * dir * speed * Time.deltaTime;
        Court.ClampPaddle(transform);
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
}
