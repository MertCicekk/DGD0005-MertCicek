# Week 4 — The server decides

Mert Çiçek · DGD0005 Multiplayer Programming · Homework 2

---

## Step 2

**RTT (HUD, client window):** 20 ms. Pressing W, my paddle moves in every window, mine too, about one round trip late: the input goes to the server, the server moves the paddle, the position comes back.

---

## Step 3

**The server receives `axis = 50`. Which line makes the paddle move at normal speed, and on which machine does that line run?**
`axis = Mathf.Clamp(axis, -1f, 1f);` in `ServerSetAxis`. It runs on the **server**, because `ServerSetAxis` is a `[ServerRpc]`: the client only calls it, the body runs on the server. So 50 becomes 1 before it is written to `_serverAxis`.

---

## Step 4

**Why 30 ms?** At tick rate 30 one tick is about 33 ms, and a keyboard sends exactly one input per tick. 30 ms is just under one tick, so every honest input passes, but a second input inside the same tick is not a keyboard, it is a program, and the server refuses it.

**What happens with 5 ms?** The limit is far below one tick, so it no longer means "one input per tick": a program could send up to 200 inputs a second and the server would accept them. Almost no protection.

**What happens with 500 ms?** The limit is much bigger than one tick, so the server accepts only 2 inputs a second and rejects about 14 of every 15 honest ones. My own paddle stutters and reacts up to half a second late (it keeps moving after I let go of W), and the rejected counter climbs during normal play.

The timestamp `_lastInputTime` is an **instance** field, not `static`: one per paddle = one per client, so my inputs never block the other player's paddle.
