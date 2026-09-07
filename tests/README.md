# POC Test Procedure

## 1. Streamer.bot WebSocket

In Streamer.bot, enable the WebSocket Server with its default local settings: host `127.0.0.1`, port `8080`, endpoint `/`.

Add this page as an OBS Browser Source:

`https://duhbuhhuh.xyz/overlays/action-replay/`

The test panel should change to **Connected to Streamer.bot WebSocket**.

## 2. Streamer.bot HTTP replay file

Enable the Streamer.bot HTTP Server. The default host is `127.0.0.1` and the default port is `7474`.

Create a mapping such as:

- Path: `replays`
- Folder: your OBS replay-buffer folder

Then use a known `.mp4` filename from that folder.

The browser-console helper can be used like this:

`testReplay("http://127.0.0.1:7474/replays/your-file.mp4")`

The video should load in the test overlay.

## 3. OBS Browser Source

The important test is not the normal desktop browser. Perform the WebSocket and video tests inside the OBS Browser Source. This confirms the actual rendering environment we plan to support.

## 4. What we are proving

We are validating the foundation only. Do not build playlist logic, replay detection, RtsUI settings, player skinning or 3D views until these local WebSocket and HTTP tests pass.
