# Action Replay Settings

This document describes the settings available in the RTS Action Replay Streamer.bot extension.

The documentation follows the same section order as the Action Replay Settings interface. Settings are described according to their current runtime behaviour.

## General

### Replay Source

These settings define where Action Replay finds local OBS Replay Buffer files and how those files are made available to the overlay.

| Setting | Description | Default |
|---|---|---|
| **Replay Folder** | Local folder containing the OBS Replay Buffer files that Action Replay can catalogue. Downloaded Twitch and Kick clips use their own separate folders and are not stored here. | Empty |
| **Replay File Types** | File extensions recognised when Action Replay scans the Replay Folder for saved replay files. Enter multiple extensions separated by commas or semicolons. A leading `.` is optional. For example: `.mp4, .mkv`. | `.mp4, .mkv` |
| **HTTP Mapping** | URL path used by Streamer.bot's HTTP server to make files from the Replay Folder available to the overlay. Enter the mapping without surrounding slashes. For example, `replays` produces the `/replays/` URL path. | `replays` |
| **HTTP Port** | Port used by Streamer.bot's HTTP server to serve replay media. This must match the port configured for Streamer.bot's HTTP server. | `7474` |

### Replay Folder

Set this to the local folder where OBS writes its Replay Buffer recordings.

When Action Replay receives a saved replay file, it checks that the file exists, is stable, has a recognised extension, and is inside the configured Replay Folder before automatically adding it to the catalog.

Downloaded Twitch and Kick clips are handled separately and are not stored in this folder.

### Replay File Types

This setting controls which file extensions Action Replay accepts when identifying local OBS replay files.

Examples:

- `.mp4`
- `.mkv`
- `.mp4, .mkv`

The comparison is case-insensitive. If an extension is entered without a leading period, Action Replay adds it automatically.

This setting does **not** convert files or change the format produced by OBS. It only controls which existing files are recognised as replay files.

### HTTP Mapping

The HTTP Mapping is the URL path that Streamer.bot uses when serving files from the Replay Folder to the overlay.

For example:

- Mapping: `replays`
- Resulting URL path: `/replays/`

The mapping must correspond to the HTTP server configuration used by Streamer.bot. It is a URL path, not the Windows filesystem path to the replay folder.

### HTTP Port

This is the TCP port used by Streamer.bot's HTTP server for replay media.

The default is `7474`.

The port configured here must match the port configured in Streamer.bot's HTTP server. Changing this value in Action Replay does not itself change Streamer.bot's HTTP server configuration.

## Configuration Notes

The four Replay Source settings work together:

1. **Replay Folder** identifies the local files.
2. **Replay File Types** determines which file extensions are recognised.
3. **HTTP Mapping** determines the URL path used to serve those files.
4. **HTTP Port** determines which HTTP server port the overlay connects to.

If the local replay folder is correct but the overlay cannot play a replay, verify the HTTP Mapping and HTTP Port against the Streamer.bot HTTP server configuration.
