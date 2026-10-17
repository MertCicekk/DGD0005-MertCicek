// Pong — Step 7. Creates the ONE ball. On the Court object, which is a scene NetworkObject.
// OnStartServer runs on the server only, once, so there is exactly one ball, and every
// client gets it from the spawn message. (Court's Awake runs on every machine: that is
// why Local Ball made three balls in Step 5.)
using FishNet.Object;
using UnityEngine;

public class BallSpawner : NetworkBehaviour
{
    public NetworkObject ballPrefab;

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (ballPrefab == null) { Debug.LogError("[Server] BallSpawner: no Ball prefab assigned."); return; }

        NetworkObject nob = Instantiate(ballPrefab, new Vector3(0, Court.BallY, 0), Quaternion.identity);
        Spawn(nob.gameObject);                     // no owner: nobody's client may move it
        Debug.Log($"[Server] ball spawned, ObjectId {nob.ObjectId}");
    }
}
