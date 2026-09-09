# Twitch Clip Catalog Integration

This branch uses Twitch as a Catalog source for Action Replay.

## Streamer.bot requirements

The Twitch C# integration requires Streamer.bot 1.0.3 or newer because Action Replay uses `TwitchGetClipDownloadUrls` to obtain the current Twitch media URL.

## Storage and HTTP mappings

Twitch downloads are **never stored in the OBS Replay Folder**. This separation is required so a downloaded Twitch clip cannot be detected by the OBS Replay Buffer file watcher as a new OBS replay.

Configure two separate Streamer.bot HTTP mappings, for example:

```text
replays  -> F:\OBS Recordings\Instant Replays
twitch   -> F:\OBS Recordings\Twitch Clips
```

The `HTTP Mapping` setting belongs to the OBS Replay Folder. `Twitch HTTP Mapping` belongs to the separate Twitch Clip Folder. Action Replay only uses the Twitch mapping for locally downloaded Twitch clips.

## Twitch playback setting

The Action Replay setting **Twitch Clip Playback** has three modes:

### Twitch URL

- Always stores the permanent Twitch Clip URL and Clip ID in the Catalog.
- Does not permanently download the clip.
- At playback time, calls `TwitchGetClipDownloadUrls` and passes the fresh Twitch media URL directly to the existing Action Replay `<video>` player.

### Download Locally

- Stores the Twitch URL and metadata in the Catalog.
- Downloads the clip to the separate Twitch Clip Folder.
- Plays the local copy through the Twitch HTTP mapping.

### Both

- Stores the Twitch URL and metadata.
- Downloads a local copy to the separate Twitch Clip Folder.
- Plays the local copy.

`Both` means **store both sources, not play twice**.

The playback mode is an Action Replay configuration value. It is deliberately **not stored on Catalog items**, so changing the setting later changes how existing Twitch Catalog items are resolved without rewriting their records.

Twitch media/download URLs are resolved when needed rather than stored permanently in the Catalog. Streamer.bot exposes `TwitchGetClipDownloadUrls(string clipId)` for this purpose.

## Actions

### `RTS - Action Replay - Twitch Clip`

Use `streamerbot/RTSActionReplayTwitch.cs` as the C# action code and attach it to the `!twitchclip` command.

Behaviour:

1. Calls Twitch `CreateClip`.
2. Adds the Twitch metadata, permanent Twitch URL and Clip ID to the Catalog.
3. If the configured mode requires local storage, downloads the clip to the separate Twitch Clip Folder.
4. Adds the Catalog ID to Recent Clips.
5. Immediately sends the clip to the normal Action Replay player using the configured playback mode.

If a clip with the same Twitch Clip ID already exists, it is not added again.

The command input is used as the clip title. If it is blank, Twitch uses the current stream title. Duration is controlled by `rts.actionreplay.twitch.clipDuration` and is constrained to Twitch's 5–60 second range.

### `RTS - Action Replay - Twitch Sync`

Use `streamerbot/RTSActionReplayTwitchSync.cs` as a separate C# action.

Schedule this action **once per hour** using Streamer.bot's Timed Action or Cron trigger.

Behaviour:

1. Calls `GetClips` for the broadcaster.
2. Checks every returned clip against Catalog `sourceType=Twitch` + `sourceId=<Twitch Clip ID>`.
3. Applies the current Twitch playback/storage setting when deciding whether to create a local copy.
4. Adds new clips to Catalog and Recent Clips.
5. Does **not** add discovered clips to the playback queue.
6. Does **not** play discovered clips.

This hourly reconciliation catches clips created directly through Twitch and prevents `!twitchclip` clips from being duplicated. `GetClips` can return up to 1,000 clips.

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
- `externalUrl`: permanent Twitch Clip URL
- `embedUrl`
- `thumbnailUrl`
- `file`: local filename when available
- `filePath`: local path when available
- `acquisitionMethod`

The Catalog does **not** contain `playbackMode`. It is an Action Replay setting.

`acquisitionMethod` is `TwitchCommand` for `!twitchclip` and `TwitchDiscovery` for the hourly reconciliation.

The persistent Catalog is separate from `recentIds`. `Maximum Recent Clips` controls the size of `recentIds`; aging out of Recent Clips does not delete the Catalog entry or the downloaded media.

## Current migration note

The existing replay player still has a legacy `replays` projection. Twitch entries now carry their Twitch source information into that projection so normal replay selection can resolve the current playback mode. The long-term migration is to have the player and OBS ingestion read/write the Catalog directly, removing the legacy projection once the Catalog UI and unified Playlist are migrated.
