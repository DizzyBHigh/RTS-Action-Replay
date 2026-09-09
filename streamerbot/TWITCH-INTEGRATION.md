# Twitch Clip Catalog Integration

This branch uses Twitch as a Catalog source for Action Replay.

## Streamer.bot requirements

The Twitch C# integration requires Streamer.bot 1.0.3 or newer because Action Replay uses `TwitchGetClipDownloadUrls` to obtain a downloadable Twitch Clip URL.

## Actions

### `RTS - Action Replay - Twitch Clip`

Use `streamerbot/RTSActionReplayTwitch.cs` as the C# action code and attach it to the `!twitchclip` command.

Behaviour:

1. Calls Twitch `CreateClip`.
2. Retries the Twitch download URL until the clip is available.
3. Downloads the landscape clip (portrait is the fallback) to `<Replay Folder>\Twitch`.
4. Adds the clip to the persistent Catalog using the Twitch Clip ID as `sourceId`.
5. Adds the Catalog ID to Recent Clips.
6. Immediately sends the clip to the normal Action Replay player.

If a clip with the same Twitch Clip ID already exists, it is not added again.

The command input is used as the clip title. If it is blank, Twitch uses the current stream title. Duration is controlled by `rts.actionreplay.twitch.clipDuration` and is constrained to Twitch's 5–60 second range.

### `RTS - Action Replay - Twitch Sync`

Use `streamerbot/RTSActionReplayTwitchSync.cs` as a separate C# action.

Schedule this action **once per hour** using Streamer.bot's Timed Action or Cron trigger.

Behaviour:

1. Calls `GetClips` for the broadcaster.
2. Checks every returned clip against Catalog `sourceType=Twitch` + `sourceId=<Twitch Clip ID>`.
3. Downloads only clips not already in the Catalog.
4. Adds new clips to Catalog and Recent Clips.
5. Does **not** add discovered clips to the playback queue.
6. Does **not** play discovered clips.

This hourly reconciliation catches clips created directly through Twitch and prevents `!twitchclip` clips from being duplicated.

## Data model

Twitch Catalog entries contain at least:

- `sourceType`: `Twitch`
- `sourceId`: Twitch Clip ID
- `title`
- `creator`
- `broadcaster`
- `captured`
- `duration`
- `viewCount`
- `gameId`
- `language`
- `externalUrl`
- `embedUrl`
- `thumbnailUrl`
- `file`
- `filePath`
- `acquisitionMethod`

`acquisitionMethod` is `TwitchCommand` for `!twitchclip` and `TwitchDiscovery` for the hourly reconciliation.

The persistent Catalog is separate from `recentIds`. `Maximum Recent Clips` controls the size of `recentIds`; aging out of Recent Clips does not delete the Catalog entry or the downloaded media.

## Current migration note

The existing replay player still has a legacy `replays` projection. `RTSActionReplayTwitch.cs` updates that projection for command-created clips so immediate playback works with the current player. The long-term migration is to have the player and OBS ingestion read/write the Catalog directly, removing the legacy projection once the Catalog UI and unified Playlist are migrated.
