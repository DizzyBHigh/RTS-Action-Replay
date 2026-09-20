# Action Replay Settings

This is the reference for the settings in RTS Action Replay. It follows the same order as the settings window, so it should be easy to find a setting while you're configuring the extension.

## General

### Settings Theme

Controls the RtsUI theme used by the Action Replay settings window. The default is **Dark**.

This changes the appearance of the settings UI only; it does not change the Action Replay overlay.

### Replay Source

The Replay Source settings tell Action Replay where OBS saves its replays and how the overlay can access those files.

| Setting | What it does | Default |
|---|---|---|
| **Replay Folder** | The folder where OBS saves Replay Buffer recordings. Twitch and Kick downloads are kept in their own folders instead. | Empty |
| **Replay File Types** | The file extensions Action Replay will recognise when looking for saved replays. Multiple extensions can be separated with commas or semicolons. | `.mp4, .mkv` |
| **HTTP Mapping** | The URL path Streamer.bot uses to serve files from the Replay Folder to the overlay. For example, `replays` creates `http://localhost:7474/replays/`. | `replays` |
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

### Configuration Testing

**Send Configuration to Overlay** sends the complete current Action Replay configuration to the overlay for testing.

This is useful when configuring Branding, Design, Title, Position and Animation settings because the current configuration can be previewed without starting a normal replay playback operation.

### Playback

The Playback settings control the player itself, including the viewer controls, progress bar and default playback speed.

| Setting | What it does | Default |
|---|---|---|
| **Show Controls** | Shows the interactive Play/Pause control and playback progress bar. | Off |
| **Show Progress Bar** | Shows the interactive playback progress bar. It can be clicked or dragged to seek. | On |
| **Default Playback Speed** | Speed used when a replay is loaded. `1.0` is normal speed. | `1.0` |
| **Show Visibility** | Controls when the playback speed indicator is shown. | `Only when greater or less than 1` |

### Show Controls

When enabled, the player displays its custom **Play/Pause** control and playback progress bar.

The Play/Pause control is interactive:

- **Play** explicitly starts playback.
- **Pause** toggles the current playback state to paused.

The custom controls are used for all supported replay sources, including YouTube clips.

### Show Progress Bar

Controls the visibility of the interactive playback progress bar. The bar can be clicked to seek to a position or dragged to scrub through playback.

The progress bar follows the configured **Control Colour** rather than the player frame colour.

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
- **Never** — don't show it.

### Player Appearance

These settings control the frame, controls and border styling around the replay player.

| Setting | What it does | Default |
|---|---|---|
| **Frame Colour Source** | Chooses the source of the player frame colour. | `Custom` |
| **Frame Colour** | Custom frame colour used when Frame Colour Source is set to Custom. | `#0384CBFF` |
| **Control Colour Source** | Chooses the source of the Play/Pause control and progress bar colour. | `Branding Primary` |
| **Control Colour** | Custom control colour used when Control Colour Source is set to Custom. | `#0384CBFF` |
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

### Control Colour Source

There are three choices:

- **Custom** — use the colour selected in Control Colour.
- **Branding Primary** — use the active Branding Preset primary colour.
- **Branding Secondary** — use the active Branding Preset secondary colour.

The default is **Branding Primary**.

### Control Colour

Sets the custom colour used by the Play/Pause control and playback progress bar. It is used when **Control Colour Source** is set to **Custom**.

### Border Glow

Adds a glow effect around the player border using the active player frame styling.

### Border Width

Sets the player border width from 0 to 12 pixels.

### Corner Radius

Sets how rounded the player corners are, from 0 to 48 pixels. `0` leaves the corners square.

## Playlist

### Replay Defaults

These settings control the default titles used when replays are created or automatically played.

| Setting | What it does | Default |
|---|---|---|
| **Replay Title Template** | Template used to generate the title of newly saved replays. Streamer.bot variables can be used. | `%replayName%` |
| **New Replay Display Title** | Temporary title displayed when a newly saved replay is automatically played. This does not rename the Catalog item. | `New Replay` |

### Replay Title Template

Defines the title template used when a new replay is saved. Streamer.bot variables can be included in the template.

### New Replay Display Title

Sets the temporary title shown when a newly saved replay is automatically played. Changing this value does **not** rename the corresponding Catalog item.

### Recent Clips

| Setting | What it does | Default |
|---|---|---|
| **Maximum Recent Clips** | Maximum number of entries retained in the Recent Clips and Last Played lists. Older entries remain in the Catalog. | `20` |
| **Auto-add Saved Replays** | Automatically adds newly saved OBS replays to the Catalog and Recent Clips list. | On |
| **Auto-play Newest Replay** | Automatically loads and plays a newly saved OBS replay. | Off |

### Maximum Recent Clips

Controls how many entries are retained in the **Recent Clips** and **Last Played** lists. The range is 1 to 100.

Older entries are removed from those lists but remain in the Catalog.

### Auto-add Saved Replays

When enabled, newly saved OBS replays are automatically added to the Catalog and Recent Clips list.

### Auto-play Newest Replay

When enabled, a newly saved OBS replay is automatically loaded and played.

### Live Playlist

| Setting | What it does | Default |
|---|---|---|
| **Persist Playlist Across Restarts** | Keeps the current Playlist when Streamer.bot restarts. | Off |

When disabled, the queue is cleared when Streamer.bot restarts.

## How the visual system fits together

The visual settings are split into reusable pieces rather than being configured separately for every command.

The basic flow is:

1. **Positions** define where an element can be placed.
2. **Animation Profiles** define how an element moves between those positions.
3. **Branding Presets** define colours, fonts and identity.
4. **Design Presets** define the visual treatment used by panels.
5. **Clapperboard Settings** define the appearance of message clapperboards.
6. **Behaviour** selects which of those pieces are used for a particular entry point.

This means you can change a Branding Preset or Animation Profile once and have every entry point using it pick up the change.

## Branding Presets

Branding Presets are reusable visual identities. They provide the colours, font and logo information used by the player, panels and messages.

A Branding Preset can contain:

| Setting | Purpose |
|---|---|
| **Preset Name** | Name shown in the settings UI and in selection lists. |
| **Source Platform** | Optional Twitch, Kick or YouTube association used for automatic source branding. |
| **Primary Colour** | Main branding colour. |
| **Secondary Colour** | Secondary branding colour. |
| **Player Title Colour** | Colour used for the replay title. |
| **Player Prefix / Suffix Colour** | Colour used by the title decoration. |
| **Panel List Text Colour** | Main text colour used in panels. |
| **Panel List Shadow Colour** | Shadow colour used by panel list text. |
| **Font** | Google Font used by branded text. |
| **Font Size** | Default branded text size. |
| **Logo URL** | HTTPS URL of the logo to display. |
| **Fallback Text** | Text used when no logo is available. |
| **Brand Label** | Label displayed beside the logo or fallback text. |

Branding is selected by the Behaviour settings. A single preset can therefore be shared by multiple player, panel or message entry points.

### Source platform branding

A Branding Preset can optionally be associated with **Twitch**, **Kick** or **YouTube**.

Player Behaviour also has **Use Source Platform Branding**. When this is enabled, Action Replay checks the replay's source platform and uses the matching Branding Preset when one exists. If there is no matching preset, the configured Play — Replay Branding Preset is used.

This lets the same playback entry point automatically use different branding for different clip sources.

## Design Presets

Design Presets control the visual treatment of panels. They are separate from Branding Presets:

- **Branding** answers "who does this look like?"
- **Design** answers "how is this panel presented?"

The built-in designs are:

- **Broadcast**
- **Cut**
- **Cinematic**
- **Minimal**

The current settings UI exposes detailed controls for Broadcast and Cut. Cinematic and Minimal are available as design presets but do not currently expose additional settings in this window.

### Broadcast

Broadcast provides the moving chevron-style panel treatment.

Its settings control:

- Background Source
- Background Colour
- Chevron Height
- Chevron Width
- Chevron Spacing
- Chevron Speed
- Randomisation of height, width and spacing

The Background Source can use the RTS dark blue, the active Branding Preset secondary colour, or a custom colour.

### Cut

Cut provides the block/bar panel treatment.

Its settings control:

- Background Source
- Background Colour
- Block Width
- Randomised Block Width
- Bar Height

Design Presets are selected by the Behaviour settings, so different entry points can use different panel treatments without changing the underlying panel implementation.

## Clapperboard Settings

Clapperboard Settings control the appearance of message clapperboards. They are the visual styling for the message element itself, rather than the animation that moves it.

The settings are:

| Setting | Purpose |
|---|---|
| **Board Color** | Base colour of the clapperboard. |
| **Text Color** | Colour of the message text. |
| **Stripe Light** | Light stripe colour on the clapperstick. |
| **Stripe Dark** | Dark stripe colour on the clapperstick. |
| **Accent Color** | Accent colour used by the clapperboard. |
| **Font** | Google Font used for clapperboard text. |

The clapperboard has its own **Message Animation** profiles and a Message Behaviour entry point. This keeps appearance, positioning and movement separate.

## Title Presets

Title Presets define reusable Title Behaviour for player presentations.

| Setting | Purpose |
|---|---|
| **Preset Name** | Display name for the preset. |
| **Decoration Position** | Places the title decoration before or after the title. |
| **Title Position** | Places the replay title at the top or bottom of the video. |
| **Decoration** | Text added before or after the replay title. |
| **Animation** | Animation used when the title enters and leaves the player. |
| **Show Delay** | Delay before the title is shown, in milliseconds. |
| **Display Duration** | How long the title remains displayed, in milliseconds. |
| **Animation Duration** | Duration of the title entrance/exit animation, in milliseconds. |

### Decoration Position

Choose **Prefix** or **Suffix** to place the decoration before or after the replay title.

### Title Position

Choose **Top** or **Bottom** to place the replay title within the player.

### Decoration

Sets the text added before or after the replay title.

### Animation

Choose the title animation:

- **Left to right**
- **Right to left**
- **Slide up/down**

### Timing

**Show Delay** ranges from 0 to 60,000 milliseconds.

**Display Duration** ranges from 0 to 120,000 milliseconds.

**Animation Duration** ranges from 0 to 10,000 milliseconds.

The **Default** Title Preset is permanent. Additional Title Presets can be created and removed.

## Positions

Positions are reusable 3D transforms. They describe where an element should appear rather than how it gets there.

Action Replay maintains three position sets:

- **Player Positions** — positions for the replay video.
- **Panel Positions** — positions for search, recent, playlist and leaderboard panels.
- **Message Positions** — positions for clapperboard messages.

Each position contains the transform information used by the overlay, including:

- Scale
- X / Y / Z position
- X / Y / Z rotation
- Field of View

Positions have both a display **name** and a runtime **tag**. The tag is the compact identifier used by runtime animation data and commands.

The settings window includes a preview for each position type, so a saved position can be checked without having to start a normal playback operation.

### Positions and animations

Animation Profiles reference saved positions rather than storing their own copies of the 3D transform.

That means changing a saved position also changes every animation profile that uses that position.

For example:

**Full Screen → Mini Player**

is an animation sequence made from two saved Player Positions. If the Mini Player position is later adjusted, the animation uses the updated position automatically.

## Animation Profiles

Animation Profiles define movement sequences for the Player, Panels and Messages.

There are three profile types:

- **Player Animation**
- **Panel Animation**
- **Message Animation**

Each profile has a **Start Sequence** and an **End Sequence**.

A sequence is a list of steps. Each step selects:

- **Position** — the saved position used for that step.
- **Duration** — how long the movement to that position takes, in milliseconds.
- **Easing** — the timing curve used for the movement.
- **Delay** — how long to wait before the next animation step starts.

Available easing options are:

- `linear`
- `ease`
- `ease-in`
- `ease-out`
- `ease-in-out`

### Start and End sequences

The **Start Sequence** controls how the element appears.

The **End Sequence** controls how it leaves.

A sequence can contain more than one position, which allows multi-step movements rather than a simple move from A to B.

For example, a sequence can move:

**Full Screen → Mini Player**

using multiple saved positions, each with its own duration and easing.

The Default profile is permanent and can be edited. Additional profiles can be created and removed.

### Animation storage

Animation profiles store position references rather than duplicating position data. When Action Replay sends an animation to the overlay, the saved position names are resolved to their position tags.

This keeps positions reusable and prevents the animation data from becoming a second, conflicting copy of the position configuration.

## Message Outputs

Message Outputs control the optional chat and overlay messages generated by Action Replay.

Each message has a message template, a **Chat** toggle and an **Overlay** toggle.

| Output | Default message |
|---|---|
| **Save Replay** | `Replay saved: %replayTitle%.` |
| **Name Replay** | `Replay #%replayNumber% renamed to %replayTitle%.` |
| **Play Replay** | `Playing replay #%replayNumber%: %replayTitle%.` |
| **Recent** | `%replayRecent%` |
| **Playlist** | `%replayPlaylist%` |

### Message templates

The message text can contain Streamer.bot variables appropriate to the output.

### Chat

When enabled, the message is sent to the requesting platform's chat.

### Overlay

When enabled, the message is sent to the Action Replay overlay.

These destinations are independent, so a message can be sent to chat, the overlay, both, or neither.

## Behaviour

Behaviour settings are the point where the reusable visual pieces are assembled.

An entry point selects:

- **Branding Preset**
- **Design Preset**
- **Title Preset**
- **Animation Profile**

The result is a complete presentation configuration for that particular operation.

### Player Behaviour

Player Behaviour controls the presentation used when the player is created or a replay is played.

The current entry points are:

- **Create — OBS**
- **Create — Twitch**
- **Create — YouTube**
- **Create — Kick**
- **Play — Replay**

Each entry point can select its own Branding, Design, Title and Animation settings.

The **Play — Replay** entry point can also use source-platform branding as described under Branding Presets.

### Panel Behaviour

Panel Behaviour controls the presentation used by overlay panels.

The current entry points are:

- **Recent / Search**
- **Playlist**
- **Leaderboards**

Each panel entry point selects its own Branding, Design, Title and Animation settings.

For example, the Playlist panel can use a different animation profile or design from the Recent/Search panel without changing how either panel works internally.

### Message Behaviour

Message Behaviour controls the presentation of messages shown through the clapperboard system.

Messages use:

- a **Message Animation** profile
- a **Branding Preset**

The separate **Clapperboard Settings** control the board's visual properties.

This separation means the same clapperboard appearance can be reused with different animation profiles, or the same animation profile can be reused with different branding.

### Putting it together

A typical replay presentation therefore looks roughly like this:

**Entry Point**
→ selects **Branding + Design + Title + Animation**

**Animation**
→ uses **saved Positions**

**Branding**
→ supplies colours, font and identity

**Design**
→ supplies the panel's visual treatment

**Clapperboard**
→ supplies message-specific appearance

The settings are deliberately separated this way so that presentation changes can be made once and reused across multiple operations.
