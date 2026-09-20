# Action Replay Settings

This is the reference for the settings in RTS Action Replay. It follows the same order as the settings window, so it should be easy to find a setting while you're configuring the extension.

## General

### Replay Source

The Replay Source settings tell Action Replay where OBS saves its replays and how the overlay can access those files.

| Setting | What it does | Default |
|---|---|---|
| **Replay Folder** | The folder where OBS saves Replay Buffer recordings. Twitch and Kick downloads are kept in their own folders instead. | Empty |
| **Replay File Types** | The file extensions Action Replay will recognise when looking for saved replays. Multiple extensions can be separated with commas or semicolons. | `.mp4, .mkv` |
| **HTTP Mapping** | The URL path Streamer.bot uses to serve files from the Replay Folder to the overlay. | `replays` |
| **HTTP Port** | The port Streamer.bot's HTTP server uses to serve replay files. | `7474` |

### Replay Folder

Point this at the folder OBS uses for its Replay Buffer recordings.

When a replay is saved, Action Replay checks the file before adding it to the catalog. The file must exist, have a recognised extension, have stopped changing, and be inside the configured Replay Folder.

Twitch and Kick clips that are downloaded locally do not go into this folder. They have separate folder settings in their own sections.

### Replay File Types

This tells Action Replay which file types count as local replay files.

For example:

- `.mp4`
- `.mkv`
- `.mp4, .mkv`

You can separate extensions with either commas or semicolons. The leading `.` is optional, so `mp4` and `.mp4` are treated the same way.

The check is case-insensitive.

This setting doesn't change the format OBS records in. It only controls which files Action Replay will recognise when it checks the Replay Folder.

### HTTP Mapping

The HTTP Mapping is the URL path used to make the replay files available to the overlay.

For example, with:

`replays`

the replay URL path is:

`/replays/`

Enter the mapping itself, rather than the Windows path to the folder. Don't add the surrounding `/` characters.

This needs to match the corresponding mapping in Streamer.bot's HTTP server configuration.

### HTTP Port

This is the port used by Streamer.bot's HTTP server when the overlay requests a replay.

The default is `7474`.

The value here must match the HTTP server port configured in Streamer.bot. Changing this setting in Action Replay does not change Streamer.bot's HTTP server configuration for you.

## How the four settings fit together

These settings each have a different job:

1. **Replay Folder** tells Action Replay where to find the files.
2. **Replay File Types** tells it which files to recognise.
3. **HTTP Mapping** provides the URL path for those files.
4. **HTTP Port** tells the overlay which HTTP server port to use.

If Action Replay is finding and cataloguing your replays but the overlay won't play them, the first things to check are the HTTP Mapping and HTTP Port in both places.
