# DGD0005-MertCicek

DGD0005 Multiplayer Programming · Mert Çiçek
Unity 6000.3.2f1 · FishNet 4.7.3 · Multiplayer Play Mode

## Week 3 — Pong (Homework 1)

Networked Pong on a dedicated server: two players each move their own paddle, the server moves the one ball and keeps the score. Scene: `Assets/Scenes/Pong.unity`. Week notes and answers: [`Week03/README.md`](Week03/README.md).

### My game's three things

In my Pong, each player **owns** their own paddle: it is spawned for their connection by the `PlayerSpawner`, only the owner reads the keyboard (`if (!IsOwner) return;`) and a client-authoritative `NetworkTransform` carries its position to the others. **Nobody** owns the ball: it is spawned once by the server (`BallSpawner.OnStartServer`), only the server moves it (`if (!IsServerInitialized) return;`) and a server-authoritative `NetworkTransform` sends its position to every client. **Everybody** shares the game state on the `GameState` scene object: the two scores and whose serve it is (`SyncVar<int>`) and the last six events (`SyncList<string>`), which only the server writes and every client — including a late joiner — mirrors.

### How to run server and client

1. Open `Assets/Scenes/Pong.unity` and press **Play** → **Start Server** (or tag a Multiplayer Play Mode player `Server`; `ServerBootstrap` starts it).
2. On each player instance type the server's IP in the HUD **Address** box (`127.0.0.1` on the same PC) → **Start Client**.
3. Move with **W/S** or **↑/↓**; the serving side presses **Space** to serve.

## Changes each week

- **Week 3:** FishNet project, connection logger, networked paddles (`PaddleController`), one server-moved ball (`NetworkBall`, `BallSpawner`), shared score and event feed (`GameState`), serve/goal RPCs, network statistics at tick rate 30.
- **Week 4 (Homework 2):** server authority — the owner sends only its input every tick, the server moves and clamps the paddle; input checks (NaN, clamp, spectator), a 30 ms rate limit per client, cheat keys C/X/T. Notes: [`Week04/README.md`](Week04/README.md).
- **Week 5:** Pong on a host — Start Host / Stop Host, the `[Host]` log line, `All.Add` guarded in both callbacks. Notes: [`Week05/README.md`](Week05/README.md).
- **Week 6 (Homework 3):** guest cheats fail, host cheats work (debug key G), serve limit 1 s per player with a `TargetRpc` refusal; Unity Relay — Multiplayer Services + FishyUnityTransport (`UnityTransport`, port 7770), `RelayLink`, Host/Join via Relay and the join code. Notes: [`Week06/README.md`](Week06/README.md).
