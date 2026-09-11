# RTS Action Replay

Action replay system for Streamer.bot and OBS.

Stage 1 keeps the replay library and statistics local to Streamer.bot. Replay video files remain on the user's computer and are served by Streamer.bot's HTTP Server to the RTS-hosted overlay.

## Replay library

- Persistent replay Catalog in `rts.actionreplay.data`.
- Recent Clips stored separately from the Catalog.
- File/Folder Watcher support for new OBS replay files.
- Stable replay IDs independent of playlist position.
- Configurable replay title templates using Streamer.bot variables.
- Explicit custom replay titles with case-insensitive duplicate checking.
- Persistent total and per-user playback counts.
- Replay creator and creator leaderboard.
- Playback leaderboard.
- Maximum history enforcement without deleting physical video files.
- Replay playback confirmation from the browser overlay before incrementing play counts.

## Live Playlist

The Playlist is a current queue, separate from Catalog and Recent Clips.

- New requested clips are queued rather than played immediately.
- New OBS replay captures are queued by the replay watcher action.
- Queue entries retain the video title and requester.
- `View` shows the current queue in order.
- `Remove` removes a queued item by position; the currently playing item cannot be removed.
- `Pause` stops automatic progression without interrupting the current video.
- `Resume` continues the queue or starts the first queued item when idle.
- Playback completion comes from the browser's native `video.ended` event over the existing Streamer.bot WebSocket.
- When the queue has another item and is not paused, the next replay replaces the video in-place without hiding/showing the player between items.
- Play counts are incremented only after the browser successfully starts playback.
- Playlist persistence is configurable. When enabled, the queue survives a Streamer.bot restart; when disabled, the queue is session-only. Transient playback state is never persisted.

## Video and player controls

`RTSActionReplayVideoControl.cs` exposes independent controls:

- `PauseVideo`
- `PlayVideo`
- `SetVideoSpeed`
- `HidePlayer` — pauses the current video before hiding it without resetting its position.
- `ShowPlayer` — shows the existing player and resumes the current video.

These controls are independent of Playlist pause/resume.

## Streamer.bot code modules

`RTSActionReplayStore.cs` is the shared Execute C# Code module. Give the Execute C# Code sub-action the name `RTS Action Replay Store` and expose its public methods through Execute C# Method sub-actions.

`RTSActionReplayPlayback.cs` is the playback module. Give it the name `RTS Action Replay Playback`.

`RTSActionReplayPlaylist.cs` is the live queue module. Give it the name `RTS Action Replay Playlist` and expose:

- `EnqueueCurrentReplay`
- `View`
- `Remove`
- `Pause`
- `Resume`
- `PlaybackEnded`

`RTSActionReplayVideoControl.cs` is the independent player-control module. Give it the name `RTS Action Replay Video Control` and expose:

- `PauseVideo`
- `PlayVideo`
- `SetVideoSpeed`
- `HidePlayer`
- `ShowPlayer`

`PlaybackEnded` is the target of the browser WebSocket `DoAction` request named `RTS Action Replay - Playback Ended`.

When a request creates or selects a replay for playback, the request action should execute `RTS Action Replay Playlist -> EnqueueCurrentReplay` instead of directly playing the replay. The OBS replay watcher should execute the same method after `RTS Action Replay Store -> AddReplay`.

The existing `RTS Action Replay Playback -> PlayReplay` method remains the actual playback path. Playlist playback supplies the catalog position for the queued replay, so all existing URL resolution, player settings and playback confirmation remain centralized there.

## Overlay

The production overlay remains hosted by The Road to Somewhere. It subscribes to the Streamer.bot custom event `RTS-Action Replay` and sends `DoAction` requests back to Streamer.bot for playback confirmation and playback completion.

The browser player has no user controls. Its status bar is visual-only; playback is controlled through Streamer.bot WebSocket events.

## Player positions

`rts.actionreplay.positions` stores named positions as a JSON object. Each position supports `scale`, `x`, `y`, `rotateX`, `rotateY` and `rotateZ`. `Full Screen` is the built-in fallback position.

## External editor

`streamerbot/StreamerBot.csproj` follows the current Streamer.bot external-editor setup. Set `StreamerBotPath` to the local Streamer.bot installation before building in VS Code.
