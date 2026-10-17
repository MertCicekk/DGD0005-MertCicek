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

