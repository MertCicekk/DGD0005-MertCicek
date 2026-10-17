// Pong — Step 7. The one ball. Nobody owns it: the SERVER moves it, every client receives
// its position through the NetworkTransform (Client Authoritative OFF) on the Ball prefab.
// The movement is LocalBall's, copied; what changed is WHO runs it.
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class NetworkBall : NetworkBehaviour
{
    public static NetworkBall Instance;

    /// True while the ball moves. Grey when false, white when true. Only the server writes it.
    public readonly SyncVar<bool> InPlay = new SyncVar<bool>();

    [SerializeField] private float speed = 7f;

    private Vector3 _velocity;

    private static readonly Color Waiting = new Color(0.45f, 0.45f, 0.45f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; }

    private void Awake()
    {
        InPlay.OnChange += OnInPlayChange;
    }

    private void OnInPlayChange(bool prev, bool next, bool asServer) => Paint();

    public override void OnStartServer()
    {
        base.OnStartServer();
        Instance = this;
        ResetToCentre();
        Paint();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Instance = this;
        Paint();                                   // a late joiner gets InPlay with the spawn
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!IsServerInitialized) return;          // only the server simulates the thing nobody owns
        if (!InPlay.Value) return;

        Vector3 p = transform.position + _velocity * Time.deltaTime;

        // walls
        float zLimit = Court.HalfHeight - Court.BallRadius;
        if (Mathf.Abs(p.z) > zLimit) { p.z = Mathf.Sign(p.z) * zLimit; _velocity.z = -_velocity.z; }

        // paddles: the SERVER's copies, a tick or two behind each owner's screen
        foreach (PaddleController paddle in PaddleController.All)
        {
            if (paddle == null || paddle.Side.Value == PaddleController.Spectator) continue;
            Vector3 pp = paddle.transform.position;
            bool towards = (pp.x < 0 && _velocity.x < 0) || (pp.x > 0 && _velocity.x > 0);
            if (!towards) continue;
            if (Mathf.Abs(p.x - pp.x) < 0.5f && Mathf.Abs(p.z - pp.z) < Court.PaddleHalfLength + Court.BallRadius)
            {
                _velocity.x = -_velocity.x;
                _velocity.z += (p.z - pp.z) * 3f;      // hit near the end: more angle
                _velocity = _velocity.normalized * speed;
                p.x = pp.x + (pp.x < 0 ? 0.5f : -0.5f);
            }
        }

        transform.position = p;

        // goal lines
        if (p.x < -Court.HalfWidth - 0.5f)      { ResetToCentre(); GameState.Instance.Goal(PaddleController.Right); }
        else if (p.x > Court.HalfWidth + 0.5f)  { ResetToCentre(); GameState.Instance.Goal(PaddleController.Left); }
    }

    /// Server only. Launches the ball towards the given side (0 = LEFT, 1 = RIGHT).
    public void Serve(int towardSide)
    {
        if (!IsServerInitialized) return;
        float dx = towardSide == PaddleController.Left ? -1f : 1f;
        float dz = Random.Range(-0.6f, 0.6f);
        _velocity = new Vector3(dx, 0, dz).normalized * speed;
        InPlay.Value = true;
    }

    private void ResetToCentre()
    {
        transform.position = new Vector3(0, Court.BallY, 0);
        _velocity = Vector3.zero;
        InPlay.Value = false;
    }

    private void Paint() => Court.Paint(gameObject, InPlay.Value ? Color.white : Waiting);
}
