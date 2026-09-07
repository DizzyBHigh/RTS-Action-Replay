# RTS Action Replay

Action replay overlay for Streamer.bot and OBS.

This repository is currently a proof-of-concept for testing the RTS-hosted overlay architecture before building the full extension.

## Test goals

- Load the overlay from `https://duhbuhhuh.xyz`.
- Connect from the OBS Browser Source to the local Streamer.bot WebSocket server.
- Confirm the overlay can receive Streamer.bot events.
- Load a local replay video served by the Streamer.bot HTTP server.
- Confirm the architecture works in OBS Browser Source before implementing playlist, skinning, 3D views and extension actions.

The production overlay is intended to remain hosted by the RTS website. Replay files remain local and are served by Streamer.bot.
