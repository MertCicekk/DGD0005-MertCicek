# Week 6 — The Host Cheats, and the Join Code

Mert Çiçek · DGD0005 Multiplayer Programming · Homework 3

In every step the main editor clicks **Start Host**; the Multiplayer Play Mode instance is the guest.

---

## Step 1 — The guest cheats

`PaddleController.Update`, next to C, X, T: **G** writes `GameState.Instance.LeftScore.Value = 9;` directly, no RPC.

| Key, on the guest | Host Console | What both windows show |
|---|---|---|
| C | 19 lines `Rejected input … too fast` | the paddle moves at normal speed |
| X | nothing | normal speed, never faster |
| T | nothing | jumps in the guest window only, snaps back |
| G | nothing | the host's score does not change |

**G wrote a SyncVar on the guest. Why did the host never see it?**
A SyncVar only travels server → clients; the guest is not the server, so its write stayed on the guest (FishNet does not even accept it there) and no message ever went to the host.

## Step 2 — Serve rate limit

## Step 3 — The host cheats

## Step 5 — RelayLink.cs

## Step 6 — Two buttons and a code

## Step 7 — Your partner, from another network
