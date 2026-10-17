# Week 3 — Pong connects and replicates

Mert Çiçek · DGD0005 Multiplayer Programming · Homework 1

---

## Checkpoint D (Step 4)

1. **Which component gives the `ObjectId`?**
   The `NetworkObject` on the Paddle prefab. It gives the paddle the same `ObjectId` on the server and on every client, so "paddle #N" means the same paddle everywhere.

2. **Which machine chose LEFT, and how did the client learn it?**
   The **server** chose it, in `PaddleController.OnStartServer()`: it looks through `PaddleController.All` and writes the first free side into `Side.Value`. `Side` is a `SyncVar<int>`, so its value travels to the client inside the spawn message; on the client `OnStartClient()` reads it, and `Side.OnChange` → `Place()` moves the paddle to `x = -Court.PaddleX` and paints it blue.

3. **Which line stops other clients moving your paddle?**
   The first line of `PaddleController.Update()`:
   ```csharp
   if (!IsOwner) return;
   ```
   Only the client whose connection owns the paddle reads the keyboard; on every other machine the method returns immediately.

---

## Predictions (written before Step 6)

1. **You press W, nothing moves elsewhere. Who must tell whom, in what order?**
   My client (the owner) must tell the server the new paddle position, then the server must pass it on to every other client (observer): owner → server → others.

2. **Three balls. Which machine should own the one real ball? What must a client receive to draw it?**
   The server should simulate the one real ball (nobody's client owns it); a client only needs to receive the ball's position every tick to draw it.

3. **If the server believes "my paddle is at z = 3.2" from a client, what is the first cheat?**
   A client can send any position it likes — teleport the paddle straight to wherever the ball is (or move it faster than allowed), and the server will accept it.

---

## Connection card (Step 11)

| | Direction 1 (Mert = server) | Direction 2 (Onur = server) |
|---|---|---|
| Partner | Onur Karakoyun (Windows build) | Onur Karakoyun (Windows build) |
| Network | Radmin VPN (different home internets) | Radmin VPN |
| Server IP | Mert: 26.12.34.56 | Onur: 26.78.90.12 |
| My ClientId + side | Mert: client 0, LEFT | Mert: client 1, RIGHT |
| RTT | Onur: 48 ms | Mert: 52 ms |
| Worked first try? | Yes | Yes |

---

## Design sheet (before Step 7)

| Thing | State or event? | FishNet piece | On which object? | Who writes / calls? |
|---|---|---|---|---|
| Ball position | state | NetworkTransform (client auth off) | Ball | server |
| Ball in play? | state | SyncVar\<bool\> | Ball | server |
| Two scores | state | SyncVar\<int\> ×2 | GameState | server |
| Whose serve | state | SyncVar\<int\> | GameState | server |
| "Let me serve" | event (request) | ServerRpc | Paddle | owner client |
| "LEFT scored!" to all | event | ObserversRpc | Paddle (scorer) | server |
| "YOU SERVE" to one | event | TargetRpc | Paddle (conceder) | server |
| Event feed | state | SyncList\<string\> | GameState | server |

`BallSpawner` on the `Court` object creates the one ball: it instantiates the Ball prefab at `(0, Court.BallY, 0)` in `OnStartServer()` and calls `Spawn(...)`, so it only ever runs on the server, once.
The first line of the ball's `Update` is `if (!IsServerInitialized) return;` — only the server moves the ball; clients just receive its position.

---

## Step 8

1. **Which RPC for a hit sound on all clients? Which for "not your serve" to one?**
   A hit sound for everyone is an `[ObserversRpc]` (server → all observers). "Not your serve" to one player is a `[TargetRpc]` (server → that one connection).

2. **Where is the score, who may change it?**
   In two `SyncVar<int>`s, `LeftScore` and `RightScore`, on the `GameState` scene object. Only the server may change them, in `GameState.Goal()`; clients only mirror them.

3. **Ball bounced on the server but passed through on your screen: which paddle copy did the server use, why different?**
   The server used **its own copy** of my paddle — the position my `NetworkTransform` last delivered to it, a tick or two old. My screen shows my paddle where it is *now*, so the two copies disagree; the server's copy is the one that counts.

