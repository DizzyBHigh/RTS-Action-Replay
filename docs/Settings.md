# Action Replay Settings

This is the reference for the settings in RTS Action Replay. It follows the current settings window order so each section can be found directly while configuring the extension.

## Local Capture

### Settings Theme

Controls the RtsUI theme used by the Action Replay settings window. This changes the settings UI only; it does not change the Action Replay overlay. The default is **Dark**.

### Local Capture / OBS

Configure how RTS discovers and serves local OBS Replay Buffer captures.

| Setting | What it does | Default |
|---|---|---|
| **Replay Folder** | Folder containing local OBS Replay Buffer files. Twitch and Kick downloads use separate folders. | Empty |
| **Replay File Types** | File extensions accepted when scanning or registering files from the Replay Folder. Separate multiple extensions with commas, for example `.mp4, .mkv`. | `.mp4, .mkv` |
| **HTTP Mapping** | URL path used in the local replay URL. It must match the Streamer.bot HTTP server mapping serving the Replay Folder. | `replays` |
| **HTTP Port** | Port used in the local replay URL. It must match Streamer.bot's HTTP server port. | `7474` |

### Replay Defaults

Configure the default metadata used when local OBS captures are added to the Catalog.

| Setting | What it does | Default |
|---|---|---|
| **Replay Title Template** | Generates the title of a newly added OBS replay. Available variables are `%replayName%`, `%replayDate%` and `%replayTime%`. A manually supplied title bypasses the template. | `%replayName%` |

### Local Capture Handling

Control what RTS does when an OBS Replay Buffer capture is saved.

| Setting | What it does | Default |
|---|---|---|
| **Auto-add Saved Replays (OBS Capture)** | Adds each newly saved OBS replay to the Catalog. It also becomes available to Recent Clips through the Catalog's recent history. | On |
| **Auto-play OBS Captures** | Adds a newly saved OBS replay to the playback queue for playback. | Off |

When automatic registration is disabled, existing replay files can still be registered with **Add Replay** or **Scan Replays**.

## Twitch

### Twitch Clips

Configure how Twitch clips are acquired and made available to the player.

| Setting | What it does | Default |
|---|---|---|
| **Twitch Clip Playback** | Use the Twitch URL, download locally, or support both methods. In Both mode, a local copy is preferred and Twitch is the fallback. | `Download Locally` |
| **Twitch Clip Folder** | Folder used for downloaded Twitch clips. It must be separate from the OBS Replay Folder. | Empty |
| **Twitch HTTP Mapping** | URL path used by Streamer.bot to serve downloaded Twitch clips. The shared HTTP Port is configured under Local Capture / OBS. | `twitch` |
| **Clip Duration** | Default duration used by `!twitchclip`, `!create-clip` and `!createclip` when no duration is supplied. | `30` seconds |

The allowed Twitch clip duration is 5-60 seconds. A duration supplied with the command overrides the setting within that range.

### Twitch Clip Playback

- **Twitch URL**  -  play directly from Twitch.
- **Download Locally**  -  download and play a local copy through Streamer.bot's HTTP server.
- **Both**  -  use the local copy when available and fall back to Twitch playback when it is unavailable.

## YouTube

### YouTube Clips

Configure the default duration for timestamp-based clips from the current monitored YouTube broadcast.

| Setting | What it does | Default |
|---|---|---|
| **Clip Duration** | Default duration used by `!create-clip` and `!createclip` when no duration is supplied. | `30` seconds |

The allowed duration is 5-60 seconds. Action Replay uses the current broadcast and its monitored start time to calculate the clip timestamp.

## Kick

### Kick Clips

Configure how Kick clips are acquired and made available to the player.

| Setting | What it does | Default |
|---|---|---|
| **Kick Clip Playback** | Use the Kick URL, download locally, or support both methods. In Both mode, a local copy is preferred and direct Kick playback is the fallback. | `Kick URL` |
| **Kick Clip Folder** | Folder used for downloaded Kick clips. It must be separate from the OBS Replay Folder. | Empty |
| **Kick HTTP Mapping** | URL path used by Streamer.bot to serve downloaded Kick clips. The shared HTTP Port is configured under Local Capture / OBS. | `kick` |

### Kick Clip Playback

- **Kick URL**  -  play directly from Kick.
- **Download Locally**  -  download and play a local copy through Streamer.bot's HTTP server.
- **Both**  -  use the local copy when available and fall back to direct Kick playback when necessary.

## Playback

### Playback

Control how replays start and how playback controls are displayed in the player.

| Setting | What it does | Default |
|---|---|---|
| **Show Controls** | Displays the Play/Pause control and interactive playback progress bar. | Off |
| **Show Progress Bar** | Displays the interactive playback progress bar. | On |
| **Default Playback Speed** | Playback speed used when a replay is loaded. Range 0.25-2.0. | `1.0` |
| **Show Visibility** | Controls when the playback speed indicator is displayed. | Only when greater or less than 1 |

### Player Appearance

Configure the player frame, controls and visual styling.

| Setting | What it does | Default |
|---|---|---|
| **Frame Colour Source** | Uses a Custom, Branding Primary or Branding Secondary colour for the player frame. | `Custom` |
| **Frame Colour** | Custom frame colour when Frame Colour Source is Custom. | `#0384CBFF` |
| **Control Colour Source** | Uses a Custom, Branding Primary or Branding Secondary colour for the Play/Pause control and progress bar. | `Branding Primary` |
| **Control Colour** | Custom control colour when Control Colour Source is Custom. | `#0384CBFF` |
| **Border Glow** | Adds a branded glow around the player border. | On |
| **Border Width** | Player border width in pixels. | `4` |
| **Corner Radius** | Player corner radius in pixels. | `0` |

**Change Player Branding to Clip Source** is configured later under Player Behaviour. When enabled, it can override the player frame/control branding with the active source branding.

## Catalog

The Catalog is the persistent replay library. Recent Clips and Last Played are different views/history derived from replay data, while the Playlist is the live playback queue.

### Recent Clips

| Setting | What it does | Default |
|---|---|---|
| **Maximum Recent Clips** | Maximum number of entries shown in Recent Clips and retained in Last Played history. Older Catalog entries remain available. | `10` |
| **Maximum Catalog Results** | Maximum number of results shown per Catalog/search page. The `--amount` option can request a different page size up to this limit. | `20` |

### Live Playlist

The Playlist is the live, source-agnostic playback queue. It can contain OBS captures, Twitch clips, YouTube clips and Kick clips.

| Setting | What it does | Default |
|---|---|---|
| **Persist Playlist Across Restarts** | Keeps the current Playlist queue when Streamer.bot restarts. Active playback and pause state are not persisted. | Off |

## Branding Presets

Define reusable colours, typography and branding identity used by Player, Panel, Clapperboard and Message presentation.

Each Branding Preset contains:

| Setting | Purpose |
|---|---|
| **Preset Name** | Display name used in settings and selection lists. |
| **Source Platform** | Optional Twitch, Kick or YouTube association. |
| **Primary Colour** | Primary branding colour. |
| **Secondary Colour** | Secondary branding colour. |
| **Player Title Colour** | Replay title text colour. |
| **Player Prefix / Suffix Colour** | Decorative title prefix/suffix colour. |
| **Panel Text Colour** | Panel title/list text colour. |
| **Panel List Shadow Colour** | Panel list shadow colour. |
| **Font** | Google Font used by player titles, panel titles and branding. |
| **Font Size** | Font size used by player and panel titles. |
| **Logo URL** | HTTPS URL of the branding logo. |
| **Fallback Text** | Text used when the branding logo cannot be loaded. |
| **Brand Label** | Label displayed with the logo or fallback text. |

### Source Platform

A preset can optionally be associated with Twitch, Kick or YouTube. Source-platform branding is enabled independently by Player, Panel, Message and Clapperboard Behaviour settings.

If a matching platform preset is unavailable, the relevant Behaviour setting's configured Branding Preset is used as the fallback.

## Design Presets

Define the visual treatment used by Player, Panel and Message presentation.

The built-in designs are:

- **Broadcast**
- **Cinematic**
- **Cut**
- **Minimal**

Broadcast and Cut expose additional configuration in the settings window. Cinematic and Minimal currently have no design-specific controls.

### Broadcast

Broadcast provides the animated chevron treatment.

Its settings control the background source/colour and chevron height, width, spacing, speed and randomisation.

### Cut

Cut provides the block/bar treatment.

Its settings control the background source/colour, block width, block-width randomisation and bar height.

Design Presets are assigned through the relevant Behaviour section.

## Title Presets

Define how replay titles are positioned, decorated and animated in the Player.

| Setting | Purpose |
|---|---|
| **Preset Name** | Display name for the preset. |
| **Decoration Position** | Places the decoration before or after the title. |
| **Title Position** | Places the title at the top or bottom of the player. |
| **Decoration** | Text displayed as the title prefix/suffix. |
| **Animation** | Title entrance/exit animation. |
| **Show Delay** | Delay before the title is shown, in milliseconds. |
| **Display Duration** | Time the title remains displayed, in milliseconds. |
| **Animation Duration** | Duration of the title animation, in milliseconds. |

The built-in **Default** Title Preset is permanent. Additional Title Presets can be created and removed.

## Positions

Define reusable Player, Panel, Message and Clapperboard positions in the shared 1920x1080 overlay coordinate space.

Animation Profiles reference these saved positions. Changing a saved position therefore affects every animation profile that uses it.

### Player Positions

Saved positions used by Player Animation and playback.

### Panel Positions

Saved positions used by Panel Animation and panel playback.

Additional Panel settings:

| Setting | Purpose | Default |
|---|---|---|
| **Width** | Information panel width in 1920x1080 output pixels. | `500` |
| **Height** | Information panel height in 1920x1080 output pixels. | `700` |
| **Corner Radius** | Information panel corner radius in pixels. | `0` |

### Message Positions

Saved positions used by Message Animation and playback.

Additional Message settings:

| Setting | Purpose | Default |
|---|---|---|
| **Width** | Fixed message width in 1920x1080 output pixels. | `500` |
| **Height** | Fixed message height in 1920x1080 output pixels. | `120` |
| **Corner Radius** | Message panel corner radius in pixels. | `0` |

Message text scales to fit the configured message dimensions.

### Clapperboard Positions

Saved positions used by Clapperboard Animation and playback.

All four position editors support saved transforms including scale, X/Y/Z position and X/Y/Z rotation. The player editor also supports the player field-of-view transform used by the overlay.

## Player Animation Profiles

Define reusable Player animation profiles with separate Start and End sequences.

Each sequence contains steps with:

- **Position**  -  a saved Player Position.
- **Duration**  -  movement duration in milliseconds.
- **Easing**  -  `linear`, `ease`, `ease-in`, `ease-out` or `ease-in-out`.
- **Delay**  -  delay before the next step, in milliseconds.

Duration and delay support 0-60,000 milliseconds.

The built-in **Default** profile is permanent and editable. Additional profiles can be created and removed.

Profiles are assigned through Player Behaviour.

## Panel Animation Profiles

Define reusable Panel animation profiles with separate Start and End sequences. Steps use saved Panel Positions with configurable duration, easing and delay.

The built-in **Default** profile is permanent and editable. Additional profiles can be created and removed.

Profiles are assigned through Panel Behaviour.

## Message Animation Profiles

Define reusable Message animation profiles with separate Start and End sequences. Steps use saved Message Positions with configurable duration, easing and delay.

The built-in **Default** profile is permanent and editable. Additional profiles can be created and removed.

The selected profile is used by Message Behaviour.

## Clapperboard Animation Profiles

Define reusable Clapperboard animation profiles with separate Start and End sequences. Steps use saved Clapperboard Positions with configurable duration, easing and delay.

The built-in **Default** profile is permanent and editable. Additional profiles can be created and removed.

The selected profile is used by Clapperboard Behaviour.

## Player Behaviour

Configure the Player presentation for each replay entry point:

- **Create - OBS**
- **Create - Twitch**
- **Create - YouTube**
- **Create - Kick**
- **Play - Replay**

Each entry point selects:

- **Animation Profile**
- **Design Preset**
- **Title Preset**
- **Branding Preset**

### Use Source Platform Branding

For newly created clips, this selects the Branding Preset associated with the clip's source platform when a matching preset exists. If no matching platform preset exists, the Play - Replay Branding Preset is used.

### Change Player Branding to Clip Source

This Play - Replay option is separate from Use Source Platform Branding. When enabled, each replay uses the Branding Preset assigned to the replay creator's platform. The player frame, design colours, buttons and progress bar follow that branding. When disabled, the Play - Replay Branding Preset is always used.

## Panel Behaviour

Configure the presentation for:

- **Recent / Search**
- **Playlist**
- **Leaderboards**

Each entry point selects an Animation Profile, Design Preset, Title Preset and Branding Preset.

**Use Source Platform Branding** can override the configured branding when a matching platform preset exists. The platform is the platform that initiated the panel command.

## Message Behaviour

Configure how transient messages are presented in the overlay.

Messages use fixed dimensions configured under **Message Positions**, scale text to fit those dimensions, and select:

- **Branding Preset**
- **Design Preset**
- **Animation Profile**
- **Display Time**
- **Use Source Platform Branding**

**Display Time** controls how long the message remains visible before its end animation begins. The default is 5000 milliseconds.

Source-platform branding uses the platform that initiated the message. If no matching platform Branding Preset exists, the configured Message Behaviour Branding Preset is used.

## Clapperboard Behaviour

Configure the Clapperboard used when a new replay is created.

| Setting | Purpose | Default |
|---|---|---|
| **Use Clapperboard** | Uses the Clapperboard for Replay Created instead of the Message system when Replay Created Overlay is enabled. | On |
| **Use Source Platform Branding** | Uses the Branding Preset associated with the platform that created the clip when available. | Off |
| **Animation Profile** | Animation profile used by the Clapperboard. | Default |
| **Branding Preset** | Fallback branding used when source-platform branding is disabled or unavailable. | Default |
| **Display Time** | Time the Clapperboard remains visible before its end animation begins, in milliseconds. | `5000` |

The Clapperboard is a separate presentation from normal Message Outputs.

## Messages

### Search Presentation

Control where Catalog/search results are presented when a search command is used.

| Setting | What it does | Default |
|---|---|---|
| **Search - Panel** | Shows Catalog/search results in the Search Panel. | On |
| **Search - Chat** | Sends one formatted chat message for the search header and each result. | Off |

Both can be enabled at the same time.

### Playlist and Recent Clips Presentation

Control how Recent Clips and Playlist entries are presented in chat.

| Setting | What it does | Default |
|---|---|---|
| **Recent - Chat** | Sends one formatted chat message for each Recent Clips entry. | On |
| **Playlist - Chat** | Sends one formatted chat message for each Playlist entry. | On |
| **List Entry Format** | Format used for each list entry. | `#%listNumber% %title%  -  %creator% | %rating%/5 | %platform% | %plays% plays` |
| **Maximum Message Length** | Maximum length of each individual list entry/message. Longer entries are truncated rather than split. | `500` |

List entry variables are `%listNumber%`, `%title%`, `%creator%`, `%rating%`, `%platform%` and `%plays%`.

### Replay Messages

Replay lifecycle notifications are configured under **Replays**, **Playlist** and **Updates**.

Each event has message text and a Chat toggle. Events that support overlay output also have an Overlay toggle.

| Event | Default message |
|---|---|
| **Replay Created** | `Replay saved: %replayTitle%.` |
| **Replay Played** | `Play` |
| **Replay Queued** | `Replay queued: %replayTitle%.` |
| **Replay Removed** | `Replay removed: %replayTitle%.` |
| **Playlist Cleared** | `Playlist cleared: %clearedCount% waiting item(s) removed.` |
| **Playlist Cleared All** | `Playlist completely cleared: %clearedCount% item(s) removed.` |
| **Replay Rated** | `Rated %replayTitle% %replayRating%/5 (average %averageRating%/5).` |
| **Replay Renamed** | `Replay #%replayNumber% renamed from %oldTitle% to %newTitle%.` |
| **Replay Deleted** | `Replay deleted: %replayTitle%.` |

Replay Created uses the separate Clapperboard presentation when **Use Clapperboard** is enabled. The other overlay-capable lifecycle events use the Message presentation.

### Replay Created variables

- `%replayTitle%`
- `%replayUser%`
- `%replayPlatform%`
- `%replaySourcePlatform%`
- `%requesterName%`
- `%requesterPlatform%`

### Replay Played variables

- `%replayNumber%`
- `%replayTitle%`
- `%replayUser%`
- `%replayPlatform%`
- `%replaySourcePlatform%`
- `%requesterName%`
- `%requesterPlatform%`

### Replay Queued variables

- `%replayTitle%`
- `%replayUser%`
- `%replayPlatform%`
- `%replaySourcePlatform%`
- `%requesterName%`
- `%requesterPlatform%`

### Replay Removed variables

- `%replayTitle%`
- `%requesterName%`
- `%requesterPlatform%`

### Playlist Cleared variables

- `%clearedCount%`
- `%requesterName%`
- `%requesterPlatform%`

### Playlist Cleared All variables

- `%clearedCount%`
- `%requesterName%`
- `%requesterPlatform%`

### Replay Rated variables

- `%replayTitle%`
- `%replayRating%`
- `%averageRating%`
- `%requesterName%`
- `%requesterPlatform%`

### Replay Renamed variables

- `%replayNumber%`
- `%oldTitle%`
- `%newTitle%`
- `%requesterName%`
- `%requesterPlatform%`

### Replay Deleted variables

- `%replayTitle%`
- `%replayUser%`
- `%replayPlatform%`
- `%replaySourcePlatform%`
- `%requesterName%`
- `%requesterPlatform%`

Overlay messages are queued centrally. Chat messages are sent immediately. An overlay message is considered finished only when its exit animation completes.

**Replay Queued** is generated only when the Playlist already contained an item before the new replay was added.

## Import / Export

### Import Settings

Import selected RTS Action Replay settings from a JSON file.

The Import dialog controls which configuration areas, profiles and Catalog data are included and handles duplicate profiles.

### Export Settings

Export the complete RTS Action Replay configuration to a JSON file. Export includes the configuration snapshot and supported global settings.

### Factory Reset

Restore RTS Action Replay configuration to its built-in defaults.

Factory Reset preserves the Catalog and play history. Configuration globals are reset and required defaults are recreated.

## Testing

### Configuration Sync

**Send Configuration to Overlay** sends the complete current Action Replay configuration to the overlay for configuration-sync testing.

### Player

**Test Origin** selects the simulated requester platform and chat routing.

**Replay Origin** independently selects the source platform of the test replay.

**Test Video** plays the first Catalog replay using the current Player presentation settings without adding it to the Playlist or play history.

**Test Panel** shows a representative panel using the first Catalog replay and current Panel presentation settings.

**Test Clapperboard** shows the Clapperboard for the first Catalog replay using the current Clapperboard presentation settings.

### Messages

**Message Type** selects the lifecycle message simulated by Test Message.

**Test Source** selects either the current Message settings or the selected Design and Branding overrides.

When Test Source is **Override**, **Message Design** and **Message Branding** select the presentation used for the test.

**Test Message** shows the selected message using the first Catalog replay. Chat output follows the selected Test Origin.
