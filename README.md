# RTS Action Replay

Action replay system for Streamer.bot and OBS, Meld.

## Replay library

- Persistent replay Catalog in `rts.actionreplay.data`.
- Recent Clips stored separately from the Catalog.
- File/Folder Watcher support for new OBS replay files.
- Stable replay IDs independent of playlist / search list position.
- Configurable replay title templates.
- Explicit custom replay titles with case-insensitive duplicate checking.
- Persistent total and per-user playback counts.
- Replay creator leaderboard.
- Playback leaderboard.
- Maximum history / recently viewed enforcement without deleting physical video files.

## Live Playlist

The Playlist is a current queue, separate from Catalog and Recent Clips.

- New requested clips are queued rather than played immediately.
- New OBS replay captures are queued by the replay watcher action.
- Queue entries retain the video title and requester.
- `!playlist` shows the current queue in order.
- `!playlist-remove <item>` removes a queued item by position; the currently playing item cannot be removed.
- `!playlist-pause` stops automatic progression without interrupting the current video.
- `!playlist-resume` continues the queue or starts the first queued item when idle.
- When the queue has another item and is not paused, the next replay replaces the video in-place without hiding/showing the player between items.
- Play counts are incremented only after the browser successfully starts playback.
- Playlist persistence is configurable. When enabled, the queue survives a Streamer.bot restart; when disabled, the queue is session-only. Transient playback state is never persisted.

## Video and player controls
- `!player-pause`
- `!player-play`
- `!player-speed <speed> 1 0 normal playback speed, 0.5 half speed, 1.5 fast-forward.`
- `!player-hide` — pauses the current video before hiding it without resetting its position.
- `!player-show` — shows the existing player and resumes the current video.
-  `!set-pos <name> [duration]`. The default transition is 1000ms; supplying a duration overrides it for that command only. Position changes do not alter Playlist state or replay identity.

## Animation profiles

Animation Profiles are presentation presets, separate from the Playlist. Positions remain reusable layouts; profiles describe how the player moves through those positions.

The initial presets are `Mini Player`, `Full Screen` and `Half Screen`. Their display names can be changed without changing their internal IDs.

Each profile has independent `Start Sequence` and `End Sequence` JSON arrays. A sequence step contains:

- `position` — saved position name or tag.
- `duration` — transition time into this position, in milliseconds.
- `delay` — time to wait after arriving before the next step, in milliseconds.
- `easing` — `linear`, `ease`, `ease-in`, `ease-out` or `ease-in-out`.

For example:

```json
[
  {"position":"Mini Hidden","duration":0,"delay":0,"easing":"ease-in-out"},
  {"position":"Mini Angled","duration":1000,"delay":3000,"easing":"ease-in-out"},
  {"position":"Full Screen","duration":1000,"delay":0,"easing":"ease-in-out"}
]
```

The first Start Sequence step establishes the initial position. Later steps transition to their target. End Sequence steps transition from the current position when the player is hidden. A queued replay that replaces an already-visible player skips the Start animation so the player does not disappear between queue items.

## Streamer.bot code modules

When a request creates or selects a replay for playback, the request action should execute `RTS Action Replay Playlist -> EnqueueCurrentReplay` instead of directly playing the replay. The OBS replay watcher should execute the same method after `RTS Action Replay Store -> AddReplay`.

The existing `RTS Action Replay Playback -> PlayReplay` method remains the actual playback path. Playlist playback supplies the catalog position for the queued replay, so URL resolution, player settings and playback confirmation remain centralized there.

## Overlay

The production overlay remains hosted by The Road to Somewhere. It can be downloaded and run locally if required.
It subscribes to the Streamer.bot custom event `RTS-Action Replay` and sends `DoAction` requests back to Streamer.bot for playback confirmation and playback completion.

The browser player has no user controls. Its status bar is visual-only; playback is controlled through Streamer.bot WebSocket events.

## Player positions

`rts.actionreplay.positions` stores named positions as a JSON object. Each position supports `scale`, `x`, `y`, `rotateX`, `rotateY`, `rotateZ` and `fov`. `Full Screen` is the built-in fallback position.

## Documentation

The user-facing documentation lives in `docs/`.

- [Commands](docs/Commands.md) — chat commands, aliases, parameters and command behaviour.
- [Settings](docs/Settings.md) — the full settings reference, following the order of the Action Replay settings window.

The command reference follows the Streamer.bot action groups. 

Internal Core actions are kept in an appendix rather than mixed into the normal chat command reference.
