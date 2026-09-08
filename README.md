# RTS Action Replay

Action replay system for Streamer.bot and OBS.

Stage 1 keeps the replay library and statistics local to Streamer.bot. Replay video files remain on the user's computer and are served by Streamer.bot's HTTP Server to the RTS-hosted overlay.

## Stage 1

- Persistent replay library in `rts.actionreplay.data`.
- File/Folder Watcher support for new OBS replay files.
- Stable replay IDs independent of playlist position.
- Configurable replay title templates using Streamer.bot variables.
- Explicit custom replay titles with case-insensitive duplicate checking.
- `!playlist` support.
- `!playreplay <number>` and `!playreplay <custom title>` support.
- Persistent total and per-user playback counts.
- Replay creator and creator leaderboard.
- Playback leaderboard.
- Maximum history enforcement without deleting physical video files.
- Replay playback confirmation from the browser overlay before incrementing play counts.
- Skinnable player frame with frame/border styling, playback speed and non-interactive status controls.
- Named player positions with scale, X/Y placement and X/Y/Z rotation.
- Streamer.bot-controlled position changes with smooth transitions.
- Configurable player entrance and exit animations: zoom and directional slides.

## Stage 2 boundary

Twitch Clip creation is deliberately not part of Stage 1. The replay record is platform-neutral so Twitch clip metadata can be added later without redesigning the replay library.

## Streamer.bot code modules

`RTSActionReplayStore.cs` is the shared Execute C# Code module. Give the Execute C# Code sub-action the name `RTS Action Replay Store` and expose its public methods through Execute C# Method sub-actions.

`RTSActionReplayPlayback.cs` is the playback module. Give it the name `RTS Action Replay Playback`.

The main public methods are:

- Store: `Initialize`, `AddReplay`, `NameReplay`, `ListPlaylist`, `CreatorLeaderboard`, `PlaybackLeaderboard`
- Playback: `SaveReplay`, `PlayReplay`, `SetPlayerPosition`, `HidePlayer`, `ConfirmPlayback`

The production import should wire the File/Folder Watcher, commands, player movement methods, custom event and overlay confirmation to these methods. Import code is intentionally not being maintained until the extension functionality is finished.

## Overlay

The production overlay remains hosted by The Road to Somewhere. It subscribes to the Streamer.bot custom event `RTS-Action Replay` and sends a `DoAction` request back to Streamer.bot using the action name `RTS Action Replay - Playback Confirm` after a replay successfully starts.

The browser player has no user controls. Its status bar is visual-only; playback is controlled through Streamer.bot WebSocket events.

## Player positions

`rts.actionreplay.positions` stores named positions as a JSON object. Each position supports `scale`, `x`, `y`, `rotateX`, `rotateY` and `rotateZ`. `Full Screen` is the built-in fallback position.

## External editor

`streamerbot/StreamerBot.csproj` follows the current Streamer.bot external-editor setup. Set `StreamerBotPath` to the local Streamer.bot installation before building in VS Code.
