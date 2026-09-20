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

## Twitch

### Twitch Clips

Twitch clips can be played directly from Twitch, downloaded to your PC, or handled using both methods.

| Setting | What it does | Default |
|---|---|---|
| **Twitch Clip Playback** | Chooses whether clips use the Twitch media URL, a local copy, or both. | `Download Locally` |
| **Twitch Clip Folder** | Folder used when Twitch clips are downloaded locally. | Empty |
| **Twitch HTTP Mapping** | URL path used by Streamer.bot to serve downloaded Twitch clips to the overlay. | `twitch` |
| **Clip Duration** | Default length used by `!clip` when no duration is given. | `30` seconds |

### Twitch Clip Playback

There are three choices:

- **Twitch URL** — play the clip directly from Twitch. No local copy is required.
- **Download Locally** — download the clip and play the local copy through Streamer.bot's HTTP server.
- **Both** — keep a local copy available, while still allowing Action Replay to fall back to the Twitch URL if a local copy can't be used.

When a local copy is required, Action Replay downloads it as an MP4 into the Twitch Clip Folder. The Twitch Clip Folder must not be the same folder as the OBS Replay Folder.

### Twitch Clip Folder

Set this to the folder where downloaded Twitch clips should be stored.

This folder is only for Twitch downloads. It should be separate from the folder used by OBS for Replay Buffer files.

If the folder is empty, local playback can't be used. Twitch URL playback can still work when the selected playback mode allows it.

### Twitch HTTP Mapping

This is the URL path Streamer.bot uses when serving downloaded Twitch clips to the overlay.

For example, with:

`twitch`

the local clip URL uses the `/twitch/` path.

The mapping is used together with the main **HTTP Port** setting under General → Replay Source. It should point to the Twitch Clip Folder in Streamer.bot's HTTP server configuration.

### Clip Duration

This is the default duration used by `!clip` when no duration is supplied.

The normal range is 5 to 60 seconds. If a duration is supplied with the command, that value is used instead, within the same 5–60 second range.

For example:

`!clip 45`

creates a 45-second Twitch clip.

The duration setting is only the default; it does not limit the length of every Twitch clip that has already been added to the catalog.
## YouTube

### YouTube Clips

YouTube clips are created as timestamped clips from the current live broadcast. There is one setting for this section: the default clip duration.

| Setting | What it does | Default |
|---|---|---|
| **Clip Duration** | Default length used by `!Create-clip` when no duration is supplied. | `30` seconds |

### Clip Duration

This is the default length used when `!Create-clip` is used without specifying a duration.

The allowed range is 5 to 60 seconds. If a duration is included with the command, that value is used instead, within the same range.

For example:

`!Create-clip`

uses the configured default.

`!Create-clip 45`

creates a 45-second clip.

A title can also be supplied after the duration:

`!Create-clip 45 Great moment`

The clip is created from the current YouTube broadcast. Action Replay uses the recorded broadcast start time to work out the timestamp for the requested clip.

If Action Replay cannot identify the current broadcast or its start time, the clip cannot be created.

## Kick

### Kick Clips

Kick clips can be captured from Kick chat using the KickBot clip workflow, or captured directly from a Kick clip URL. When a clip is downloaded locally, Action Replay uses the Kick Clip Folder and Kick HTTP Mapping settings.

| Setting | What it does | Default |
|---|---|---|
| **Kick Clip Playback** | Chooses whether Kick clips use the Kick URL, a local copy, or both. | `Kick URL` |
| **Kick Clip Folder** | Folder used when Kick clips are downloaded locally. | Empty |
| **Kick HTTP Mapping** | URL path used by Streamer.bot to serve downloaded Kick clips to the overlay. | `kick` |

### Kick Clip Playback

There are three choices:

- **Kick URL** — use the Kick clip URL for playback.
- **Download Locally** — keep a local copy for playback.
- **Both** — support a local copy while retaining the Kick URL as a fallback.

The setting controls how locally acquired Kick clips are made available for playback.

### Kick Clip Folder

Set this to the folder where downloaded Kick clips should be stored.

This folder is separate from the OBS Replay Folder. The setting is only relevant when the selected playback mode needs a local copy.

### Kick HTTP Mapping

This is the URL path used when Streamer.bot serves a downloaded Kick clip to the overlay.

For example, with:

`kick`

the local clip URL uses the `/kick/` path.

The mapping is used with the main **HTTP Port** setting under General → Replay Source and must point to the Kick Clip Folder in Streamer.bot's HTTP server configuration.

### Kick clip commands

Kick uses `!create-clip` for clip creation. The duration is optional and defaults to 30 seconds.

For example:

`!create-clip`

uses the default duration.

`!create-clip 45`

requests a 45-second clip.

A title can be supplied after the duration:

`!create-clip 45 Great moment`

KickBot requests are limited to 5–240 seconds by the command parser. The locally configured **Kick Clip Playback** mode determines how the resulting clip is made available to Action Replay.

## Player

### Playback

The Playback settings control the player itself and the default speed used when a replay is loaded.

| Setting | What it does | Default |
|---|---|---|
| **Show Controls** | Shows the player status bar on the overlay. The bar is visual only and is not interactive. | Off |
| **Show Progress Bar** | Shows the playback progress bar. It is visual only. | On |
| **Default Playback Speed** | Speed used when a replay is loaded. `1.0` is normal speed. | `1.0` |
| **Show Visibility** | Controls when the playback speed indicator is shown. | `Only when greater or less than 1` |

### Show Controls

When enabled, the player displays its status bar. The controls are for display only; they are not clickable controls for the viewer.

### Show Progress Bar

Controls the visibility of the non-interactive progress bar shown during playback.

### Default Playback Speed

Sets the speed used when a replay is loaded. The available range is 0.25 to 2.0, in 0.25 steps.

Examples:

- `0.5` — half speed
- `1.0` — normal speed
- `2.0` — double speed

The setting is the starting playback speed. It does not prevent the video speed from being changed later with the video speed command.

### Show Visibility

Choose when the playback speed indicator is displayed:

- **Always** — show the indicator at all times.
- **Only when greater or less than 1** — show it when playback is slower or faster than normal speed.
- **Never** — don't show the indicator.

### Player Appearance

These settings control the frame around the replay player.

| Setting | What it does | Default |
|---|---|---|
| **Frame Colour Source** | Chooses the source of the player frame colour. | `Custom` |
| **Frame Colour** | Custom frame colour used when Frame Colour Source is set to Custom. | `#0384CBFF` |
| **Border Glow** | Adds a branded glow around the player border. | On |
| **Border Width** | Width of the player border in pixels. | `4` |
| **Corner Radius** | Rounds the player corners by the specified number of pixels. | `0` |

### Frame Colour Source

There are three choices:

- **Custom** — use the colour selected in Frame Colour.
- **Branding Primary** — use the active Branding Preset primary colour.
- **Branding Secondary** — use the active Branding Preset secondary colour.

### Frame Colour

Sets the custom player frame colour. This is used when **Frame Colour Source** is set to **Custom**.

### Border Glow

Adds a glow effect around the player border using the active player frame styling.

### Border Width

Sets the player border width from 0 to 12 pixels.

### Corner Radius

Sets how rounded the player corners are, from 0 to 48 pixels. `0` leaves the corners square.
