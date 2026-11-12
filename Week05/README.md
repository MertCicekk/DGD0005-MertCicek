# Week 5 — Pong on a host

Mert Çiçek · DGD0005 Multiplayer Programming · In-Class Assignment

---

## Step 2 — Start Host / Stop Host and the `[Host]` line

- `ConnectionHud.OnGUI`: **Start Host** (only when server and client are both Stopped) calls `_nm.ServerManager.StartConnection();` then `_nm.ClientManager.StartConnection("127.0.0.1", port);` with the Tugboat port (7770). **Stop Host** (only when both are Started) stops the client, then the server.
- `ConnectionLogger.OnClientConnectionState`: when the client state is `Started` and `_nm.ServerManager.Started` is true it logs `[Host] This process is the server AND a client. RTT is 0 ms.` It is checked in the client handler because the server is already started by then.

Console after one click on Start Host:

```
[Server] Starting
[Client] Starting
[Server] Started
[Server] Client 0 connected
[Client] Started
[Host] This process is the server AND a client. RTT is 0 ms.
[Client] Connected, the server calls me client 0
[Server] Spawned paddle #3 for client 0: LEFT
[Client] I am client 0, playing LEFT. My paddle is #3
```

HUD on the host: `Server: 1 client(s)`, `Client: my id = 0`, `RTT (ping): 33 ms` (one tick; see self-review 1).

---

## Step 3 — Both callbacks, one paddle

Both `All.Add` calls in `PaddleController` (`OnStartServer` and `OnStartClient`) are guarded with `if (!All.Contains(this))`. A HUD panel `Paddles in All: N` prints `All.Count`. On the host with one paddle it says **1**, not 2.

**Which callback ran first, which half of the host ran it?** `OnStartServer` ran first (`[Server] Spawned paddle #3 …`), run by the **server half** of the host. Then the **client half** ran `OnStartClient` on the same object #3 (`[Client] I am client 0 … My paddle is #3`). One object, two halves, two callbacks; without the guard the second one would add the same paddle again.

---

## Step 5 — The host wins

| | Host | Guest |
|---|---|---|
| ClientId | 0 | 1 |
| Side | LEFT | RIGHT |
| RTT (HUD) | 33 ms (one tick, really 0) | 8 ms (MPPM Player 3, same PC) |
| Serve starts | the instant Space is pressed | one RTT later |
| Furthest the ball got at a paddle (face at x = ±6.50) | 6.50 every bounce | 6.37–6.50, never past it |

**Guest's RTT:** 8 ms (Player 3 HUD right after connecting; 20 ms during the rally). Over the LAN with Onur: 3 ms (Step 7).

I served the ball straight (z = 0) so it bounced between the two paddles, and logged the furthest x it reached before each bounce, on the host and on the guest (11 bounces). On the host every bounce is exactly at 6.50. On the guest it turns at 6.37–6.50: a little early (the interpolation cuts the corner), but it **never passed through the paddle**.

Why not, when the sheet says it should: since Homework 2 my paddle is **server-authoritative** (input up, position down). The guest does not draw its paddle where it pressed W; it draws the server's paddle, a tick late, and the server's ball, also a tick late. Both are equally old, so they agree on screen. The pass-through happens with the Homework 1 paddle (client-authoritative): the guest draws its own paddle at once, the server bounces the ball on an older copy, and the guest sees the ball go through its paddle, then snap back. In my version the delay moved to the input: the guest's paddle and the guest's serve react one RTT after the key.

The host never waits: the server bounces the ball on the host's paddle itself, and the host's serve starts the instant Space is pressed. That is host advantage.

**A second guest, the spectator.** I activated MPPM Player 4 and connected it after Player 3. It got ClientId 2 and the side SPECTATOR (feed: `client 2 joined as SPECTATOR`). On its screen: its own paddle hidden, both LEFT and RIGHT paddles and the ball visible. I served from the host; the spectator saw the ball, then the score go to 1 : 0 and the feed line `LEFT scored. 1 : 0`. Its input does nothing: I sent `ServerSetAxis(1)` from the spectator, and on the server its paddle stayed at z = 0.00, because `ServerSetAxis` returns when `Side` is SPECTATOR (and `OnTick` does not even send input for a spectator).

**Tick Rate 10, one rally, the ball on the guest:**

I logged the ball's position on the guest (Player 3) every frame for 1.5 s after a serve:

| Tick rate | One tick | Guest RTT | Frames | Frames the ball stood still | Ball on the guest |
|---|---|---|---|---|---|
| 30 | 33 ms | 20 ms | 435 | 14 (3 %) | smooth |
| 10 | 100 ms | 80 ms | 401 | 85 (21 %) | jerky: it stops in one frame of five, then jumps on |

With 10 ticks a second the server sends the ball's position only every 100 ms, and the guest's paddle on the server is up to a tick older, so host advantage gets bigger. Set back to **30**.

---

## Step 6 — The host leaves

Tested with the main editor as host and MPPM Player 3 as guest.

**The host leaves.** Mid-rally (ball in play) I clicked Stop Host. Guest Console: `[Client] Stopping`, `[Client] Stopped`. On the guest: 0 paddles, no ball, `GameState` not spawned, so the score panel is gone. Start Host and Start Client again: host id 0, guest id 1, score 0 : 0.

**The guest leaves.** Ball in play, the guest clicked Stop Client. Host Console: `[Server] Client 1 disconnected`, feed `client 1 left (RIGHT)`. On the host: 1 paddle (LEFT), the ball back in the centre and waiting, serve LEFT. The host keeps playing.

When a player leaves, `PaddleController.OnStopServer` resets the ball to the centre (`NetworkBall.ResetForLeave`) and, if it was the leaver's serve, `GameState.PlayerLeft` hands `ServingSide` to the side that is still there, so the host can keep playing and serve.

---

## Step 7 — Connection card

| | Direction 1 (I host) | Direction 2 (partner hosts) |
|---|---|---|
| Partner | Onur | Onur |
| Network | classroom LAN, cable | classroom LAN, cable |
| Host's address | 192.168.1.12 (my laptop) | 192.168.1.7 (Onur's desktop) |
| My ClientId + side | 0, LEFT (host) | 1, RIGHT (guest) |
| My RTT | 33 ms on the HUD = one tick, tells nothing | 3 ms |
| Onur's RTT | 4 ms | 33 ms (he is the host) |
| Connected first try? | **No.** Starting → Stopped after ~10 s. Fix line 2: the host's firewall — I allowed Unity for Private + Public, then it connected on the first retry. | **Yes** — Onur's firewall was already fixed in Week 3. |

---

## Step 8 — The same scripts, three ways

1. **Start Server only:** the server half runs — `ConnectionLogger` server events, `PlayerSpawner`, `BallSpawner`, `OnStartServer`, `GameState.Goal/Announce`, the server part of `OnTick`. Nothing client-side runs: no `OnStartClient`, no `IsOwner` paddle, no keyboard, no `[Host]` line.
2. **Start Host:** everything that has a server half and a client half runs twice on the same object: `OnStartServer` + `OnStartClient`, SyncVar `OnChange` (asServer true and false), the feed `[Server]` log plus the client display, the connection logger for both sides.
3. **Start Client (guest):** it knows only an IP and a port. It does not know that the server is also a player; for the guest nothing changed since Week 3.

---

## Self-review

1. **Start Host: which two calls? Why is the host's RTT 0?**
   `ServerManager.StartConnection()` then `ClientManager.StartConnection("127.0.0.1", port)`. The host's client talks to its own server inside the same process, in memory, so a message never travels over a network: RTT 0. On the HUD the host showed 33 ms: FishNet measures RTT in whole ticks, and at tick rate 30 the smallest step it can show is one tick (33 ms). The real delay is still 0; that is why the host's RTT tells nothing.

2. **The guest's paddle on the host, the guest and a spectator.**

   | | `IsOwner` | `IsServerInitialized` | callbacks | reads the keyboard |
   |---|---|---|---|---|
   | Host | false | true | `OnStartServer`, `OnStartClient` | nobody (the host's keyboard drives only the host's paddle) |
   | Guest | true | false | `OnStartClient` | the guest |
   | Spectator | false | false | `OnStartClient` | nobody |

   Checked with host + Player 3 (guest, paddle #4) + Player 4 (spectator): #4 reads `IsOwner = False, IsServerInitialized = True` on the host, `True, False` on the guest, `False, False` on the spectator.

3. **Two serves traced.**
   Host: Space → `RequestServe()` ServerRpc → runs at once in the same process → `ball.Serve` → ball moves.
   Guest: Space → `RequestServe()` ServerRpc → sent over the network, half an RTT → server runs `ball.Serve` → new ball position comes back, half an RTT → ball moves on the guest.
   **The guest waits** (one RTT).

4. **Step 6.** The guest's own paddle vanished because the server spawned it and the server is the only one that says what exists; when the host stopped, the server stopped, the guest's client disconnected and all networked objects were despawned. When the guest stopped, only one client left: the server kept running on the host, removed the guest's paddle and kept the game. Only one machine is special.

5. **State or event.**
   score → **state** (SyncVar) · "RIGHT scored!" → **event** (ObserversRpc) · whose serve → **state** (`ServingSide` SyncVar) · "YOU SERVE" → **event** (TargetRpc) · the ball's position → **state** (NetworkTransform) · the feed → **state** (SyncList).

6. **A guest writes `GameState.Instance.LeftScore.Value = 9`.** Nothing happens, not even on the guest's own screen. In FishNet 4.7.3 the SyncVar setter calls `CanNetworkSetValues`: the server is not started on the guest and the SyncVar's write permission is server-only, so it logs a warning and returns before the value is changed. Nothing is sent. On the host the same line *works* because the host is also the server: `IsServerInitialized` is true there, it writes the server's copy and that is synced to everyone. That is why you test from a guest.

7. **Feed logs twice on the host.** A `SyncVar`/`SyncList` `OnChange` callback runs twice on the host: once with `asServer = true` (server half), once with `asServer = false` (client half). A goal sound in that callback goes behind `if (!asServer)`, so only the client half plays it. The score log does not double because only the server writes it: `GameState.Announce` logs `[Server] feed: …` and returns when `!IsServerInitialized`, and the server half runs it once. In my code the goal flash is an `ObserversRpc` (`AnnounceGoal`), which runs once per client, so on the host it also plays once.

---

## Note

The two `[Server] Rejected input … too fast` lines in the host Console come right after the start (the first ticks run close together). After that the counter stays still.
