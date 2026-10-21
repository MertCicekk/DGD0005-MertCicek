# Week 4 — The server decides

Mert Çiçek · DGD0005 Multiplayer Programming · Homework 2

---

## Step 2

**RTT (HUD, client window):** `__ ms` ← *fill in*

---

## Step 3

**The server receives `axis = 50`. Which line makes the paddle move at normal speed, and on which machine does that line run?**
`axis = Mathf.Clamp(axis, -1f, 1f);` in `ServerSetAxis`. It runs on the **server**, because `ServerSetAxis` is a `[ServerRpc]`: the client only calls it, the body runs on the server. So 50 becomes 1 before it is written to `_serverAxis`.
