# Action Replay Commands

This is the command reference for RTS Action Replay.

The groups below follow the order used by the Streamer.bot actions. Only actions with a chat command are included in the main reference. The internal Core actions are listed in Appendix A at the end.

A few commands have more than one name. Pick whichever spelling you prefer;

---

## RTS - Action Replay - Twitch

### Create Twitch Clip

Create a Twitch clip from the current Twitch stream.

**Commands**

`!twitchclip [<duration>] [<title>]`  
`!create-clip [<duration>] [<title>]`  
`!createclip [<duration>] [<title>]`

**Twitch chat only.**

**Examples**

`!twitchclip`

`!create-clip 45`

`!create-clip 45 That was not planned`

If no duration is supplied, the configured Twitch Clip Duration is used.

The allowed duration is 5–60 seconds. A title can be supplied after the optional duration.

---

## RTS - Action Replay - YouTube

### Create YouTube Clip

Create a timestamped clip from the current YouTube live broadcast.

**Commands**

`!create-clip [<duration>] [<title>]`  
`!createclip [<duration>] [<title>]`

**YouTube chat only.**

**Examples**

`!create-clip`

`!create-clip 45`

`!create-clip 45 Something happened`

If no duration is supplied, the configured YouTube Clip Duration is used.

The allowed duration is 5–60 seconds.

The title is everything after the optional duration.

Action Replay uses the recorded start time of the current YouTube broadcast to calculate the timestamp for the clip. If the current broadcast or its recorded start time cannot be identified, the clip cannot be created.

---

## RTS - Action Replay - Kick

### Create Kick Clip

Request a Kick clip through the KickBot workflow.

**Commands**

`!create-clip [<duration>] [<title>]`  
`!createclip [<duration>] [<title>]`

**Kick chat only.**

**Examples**

`!create-clip`

`!create-clip 45`

`!create-clip 45 That was close`

If no duration is supplied, the default is 30 seconds.

The command parser accepts 5–240 seconds. The title is everything after the optional duration.

The command first asks KickBot to create the clip. Action Replay then captures the returned KickBot clip and adds it to the Catalog.


## RTS - Action Replay

### Play History Item

Play an item from the Last Played history.

**Command**

`!play-history <history item>`

**Example**

`!play-history 2`

The number is the item's position in the Last Played list. Use `!last-played` first if you need to see the list.

### Play Replay

Play a replay from the Catalog.

**Commands**

`!play-clip <replay number>`  
`!playclip <replay number>`  
`!play-replay <replay number>`  
`!playreplay <replay number>`

You can select a replay in three ways:

- **Replay number** — the number shown in a Catalog/search result.
- **Custom title** — the exact custom title assigned to a replay.
- **Platform and user** — select an item from a user's search results, for example `!play-replay Twitch:SomeUser 2`.

**Examples**

`!play-replay 4`

`!play-replay Best crash ever`

`!play-replay Twitch:SomeUser 2`

When a replay is selected by number, the number refers to the current Catalog/search result set, not a permanent replay ID.

### Save Replay

Tell OBS to save the current Replay Buffer.

**Commands**

`!savereplay`  
`!save-replay`  
`!save-clip`  
`!saveclip`  
`!clip-local`  
`!cliplocal`

This asks OBS to save the Replay Buffer. What happens next depends on the Replay Source and Playlist settings.

If the save is associated with a chat user, that requester information is carried through to the saved replay.

---

## RTS - Action Replay - Catalog

The Catalog commands work with the current search/result state. Think of the Catalog as the library and these commands as the buttons you wish Streamer.bot had given you. The search is saved per user. 
If you want to interact with another user's search, you can use <platform>:<user> in your command to target the search the user made.
e.g. `!play-clip twitch:duhbuhhuh 3` would play the third clip in the list from duhbuhhuh's search results.
If the platform is omitted, the command looks for the user on the platform the person performing the command is on.

### First Page

Return to the first page of the current Catalog/search results.

**Command**

`!search-first`

This does not start a new search. It changes the page of the current result set.

### Last Page

Jump to the last page of the current Catalog/search results.

**Command**

`!search-last`

### List Last Played

Show the most recently played replays.

**Commands**

`!last-played [--amount <N>]`  
`!recently-played [--amount <N>]`

**Examples**

`!last-played`

`!last-played --amount 10`

The amount is optional. The configured Maximum Recent Clips setting is used when no amount is supplied.

This list is playback history, not the same thing as Recent Clips.

### List Recent

Show recently captured/added replays.

**Command**

`!search-recent [--amount <N>]`

**Example**

`!search-recent --amount 10`

This uses the Catalog's recent ordering. It is different from Last Played: a replay can be recent without ever having been played.

### Next Page

Move to the next page of the current Catalog/search results.

**Command**

`!search-next`

If you're already on the last page, it stays there.

### Previous Page

Move to the previous page of the current Catalog/search results.

**Command**

`!search-prev`

If you're already on the first page, it stays there.

### Rate Replay

Rate the replay that is currently playing.

**Commands**

`!rate-clip <rating>`  
`!rate-replay <rating>`  
`!rate-video <rating>`

**Rating**

`<rating>` must be from **1 to 5**.

**Examples**

`!rate-replay 5`

`!rate-video 3`

Ratings are stored per user, so rating the same replay again updates that user's rating rather than creating another rating from the same account.

### Search

Search the Catalog by text.

**Command**

`!search <search text> [--amount <N>]`

**Examples**

`!search funny`

`!search police chase --amount 10`

The search text is matched against the replay's searchable Catalog information. The optional amount controls how many results are shown per page.

Running `!search` with no search text returns the Catalog without a text filter.

### Search By Creator

Search for replays by creator.

**Commands**

`!search-creator <creator> [--amount <N>]`  
`!creator-search <creator> [--amount <N>]`

**Examples**

`!search-creator Dizzy`

`!creator-search Dizzy --amount 20`

The creator name is required.

### Search By Date

Search for replays from a date period.

**Commands**

`!search-date <date period> [--amount <N>]`  
`!date-search <date period> [--amount <N>]`

**Examples**

`!search-date today`

`!search-date this week --amount 10`

The date-period text is passed to the Catalog date filter. The exact accepted period names depend on the date parser; 
e.g. `this week`, `last week`, `this year`, `last year`, `2025`, `November`, `November 2025`

### Search By Views

List replays ordered by playback count.

**Commands**

`!search-views [--amount <N>]`  
`!most-views [--amount <N>]`

**Examples**

`!search-views`

`!most-views --amount 10`

Only replays with at least one recorded play are included.

### Add Replay

Add one existing OBS replay file from the configured Replay Folder to the Catalog.

**Command**

`!add-existing-replay <filename> [title]`

**Examples**

`!add-existing-replay replay-2026-09-20-153000.mp4`

`!add-existing-replay replay-2026-09-20-153000.mp4 That was insane`

`!add-existing-replay "My replay 2026-09-20.mp4" That was insane`

The filename is resolved against the configured Replay Folder and must use one of the configured Replay File Types. If no title is supplied, the Replay Title Template is used.

A supplied title becomes the replay's custom Catalog title.

If the replay is already in the Catalog, it is not added again.

### Scan Replays

Scan the configured Replay Folder and add replay files that are not already in the Catalog.

**Commands**

`!scan-replays`  
`!scanreplays`

The scan only considers files matching the configured Replay File Types. Existing Catalog entries are skipped.

### Search Top Rated

List replays by rating.

**Command**

`!search-rating [<rating>] [--amount <N>]`

The rating is optional and accepts **0–5**.
For ratings 1–4, the command uses a rating band:
- `0` = unrated replays.
- `1` = average rating 1.0–1.9
- `2` = 2.0–2.9
- `3` = 3.0–3.9
- `4` = 4.0–4.9
- `5` = 5.0

Without a rating, rated replays are returned ordered by average rating, with rating count used as the tie-breaker.

**Examples**

`!search-rating`

`!search-rating 4`

`!search-rating 5 --amount 10`

### Top Creators

Show the creator leaderboard.

**Commands**

`!creator-leaderboard [<period>] [--amount <N>]`  
`!top-clippers [<period>] [--amount <N>]`  
`!top-creators [<period>] [--amount <N>]`

**Examples**

`!top-creators`

`!top-creators week --amount 10`

An optional time period can be used to narrow the leaderboard. Supported period forms include `today`, `week`, `month`, `year`, `all`, and month/year values understood by the Catalog date parser.

---

## RTS - Action Replay - Playlist

The Playlist is the live queue. It lists the clips that have been requested to be played. It is separate from the Catalog and Recent Clips.

### List Playlist

Show the current Playlist.

**Command**

`!playlist`

**Example**

`!playlist`

The list shows the queue in order, including the requester where available.

If the Playlist is empty, it says so.

### Playlist - Clear

Remove the waiting items from the Playlist while leaving the currently playing item alone.

**Commands**

`!playlist-clear`  
`!playlistclear`

This clears queued items but does not stop the active replay.

### Playlist - Clear All

Remove everything from the Playlist.

**Commands**

`!playlist-clearall`  
`!playlistclearall`

This clears the queue and resets the Playlist pause state. It does **not** stop a replay that is already playing.

### Playlist - Pause

Pause automatic Playlist progression.

**Commands**

`!playlist-pause`  
`!playlistpause`

Pause does not stop the current video. It prevents the Playlist from automatically moving on to the next item.

### Playlist - Remove

Remove a waiting item by its Playlist position.

**Commands**

`!playlist-remove <playlist item>`  
`!playlistremove <playlist item>`

**Example**

`!playlist-remove 3`

The number is the item's position in the current Playlist.

The currently playing item cannot be removed.

### Playlist - Resume

Resume Playlist progression.

**Commands**

`!playlist-resume`  
`!playlistresume`

If the Playlist is paused, this allows it to continue.

If nothing is currently playing and there are queued items, Resume starts the next item.

---

## RTS - Action Replay - Store

### Name Replay

Give a Catalog replay a custom title.

**Commands**

`!name-clip <replay number> <replay title>`  
`!nameclip <replay number> <replay title>`  
`!renameclip <replay number> <replay title>`  
`!rename-clip <replay number> <replay title>`

**Examples**

`!name-clip 4 Best police chase`

`!renameclip 2 That went well`

The replay number refers to the current Catalog order.

Custom titles must be unique, ignoring case. Renaming a replay does not rename the physical video file.

### Show Creator Leaderboard

Show the creator leaderboard.

**Commands**

`!creator-leaderboard`  
`!top-creators`  
`!top-clippers`

These commands overlap with the Catalog **Top Creators** action. The leaderboard is the same general creator-ranking feature; the two Streamer.bot actions exist in the current action setup for different integration paths.

### Show Playback Leaderboard

Show the playback leaderboard.

**Commands**

`!clip-leaderboard`  
`!clipleaderboard`

This reports playback activity rather than creator totals.

---

## RTS - Action Replay - Video

These commands control the player currently shown by the RTS overlay.

### Show Player

Show the player.

**Commands**

`!show-player`  
`!showplayer`  

### Hide Player

Hide the player.

**Commands**
 
`!playerhide`  
`!player-hide`

Hiding the player does not delete or reset the replay. It sends the hide operation to the overlay.

### Pause

Pause the current video.

**Commands**

`!video-pause`  
`!videopause`  
`!player-pause`  
`!playerpause`

This controls playback of the video currently loaded in the player.

### Play

Resume/play the current video.

**Commands**

`!video-play`  
`!videoplay`  
`!player-play`  
`!playerplay`

### Screen Position

Move the player to a saved position.

**Commands**

`!player-pos <position tag> [<duration ms>]`  
`!player-position <position tag> [<duration ms>]`

**Examples**

`!player-pos full-screen`

`!player-position mini-player 750`

Use the **saved Player Position tag**, not the display name. Position names can contain spaces, but the chat command reads the first whitespace-delimited value as the position, so a multi-word display name cannot be passed reliably.

For example, the saved position **Full Screen** uses the tag `full-screen`.

The optional duration is the movement time in milliseconds. If omitted, the normal transition duration is used.

### Set Speed

Change the current playback speed.

**Commands**

`!set-speed <playback speed>`  
`!setspeed <playback speed>`  
`!player-speed <playback speed>`  
`!playerspeed <playback speed>`  
`!clip-speed <playback speed>`  
`!clipspeed <playback speed>`

**Examples**

`!set-speed 0.5`

`!player-speed 1`

`!clip-speed 2`

The command accepts a playback speed from **0.25 to 4.0**. Values outside that range are clamped.

The setting called Default Playback Speed controls the speed when a replay is loaded. This command changes the speed of the current playback.

---

# Appendix A — Core Actions

The Core actions are the machinery behind the commands. They are included here for reference, but they are not chat commands.

### Core — Catalog

**Action:** `RTS - Action Replay - Core - Catalog`

Handles Catalog searches, filtering, paging, ratings, playback history and leaderboard queries.

The user-facing Catalog commands call methods in this action rather than accessing the Catalog directly.

### Core — Playback

**Action:** `RTS - Action Replay - Core - Playback`

Handles replay saving, replay selection and player positioning.

The main `Play Replay`, `Save Replay` and `Screen Position` actions use this module.

### Core — Playlist

**Action:** `RTS - Action Replay - Core - Playlist`

Handles the live replay queue.

It adds selected replays to the queue, displays the queue, removes items, pauses/resumes progression and advances the queue when playback ends.

### Core — Resolver

**Action:** `RTS - Action Replay - Core - Resolver`

Resolves the configuration needed by the overlay before a player, panel or animation operation is sent to it.

This is internal plumbing. You should not normally need to call it yourself.

### Core — Search Queue

**Action:** `RTS - Action Replay - Core - Search Queue`

Queues search-panel requests and hands them to the overlay/search rendering path.

It also handles the internal search-panel lifecycle.

### Core — Settings

**Action:** `RTS - Action Replay - Core - Settings`

Owns the Action Replay settings/configuration interface and its persisted configuration data.

This is the bridge between the Streamer.bot settings UI and the stored Action Replay configuration.

### Core — Store

**Action:** `RTS - Action Replay - Core - Store`

Provides the shared Catalog/store functionality used by the rest of Action Replay.

It owns persistent replay data and related store operations such as adding and naming replays.

### Core — Store

**Action:** `RTS - Action Replay - Core - Store`

The Store also provides the manual replay import operations used by **Add Replay** and **Scan Replays**.

- `AddExistingReplay` — adds one existing replay file from the configured Replay Folder.
- `ScanReplays` — scans the configured Replay Folder and imports replay files missing from the Catalog.

These operations do not depend on **Auto-add Saved Replays** being enabled.

### Core — Video Controls

**Action:** `RTS - Action Replay - Core - Video Controls`

Provides the low-level player commands used by Hide, Show, Pause, Play and Set Speed.

It sends the corresponding player operation to the RTS overlay.

### Event Trigger

**Action:** `RTS - Action Replay - Core - Event Trigger`

This is the Streamer.bot custom trigger for the `RTS-Action Replay` event.

The overlay uses this event path to communicate player and panel operations back through Streamer.bot.

### Init Store

**Action:** `RTS - Action Replay - Init Store`

Initialises the persistent Action Replay store and makes sure the basic Catalog/history structures exist.

It is startup plumbing, not a chat command.

### Core — Platforms

**Action:** `RTS - Action Replay - Core - Platforms`

The platform code is exposed as its own Streamer.bot Execute Code action and is used by the Twitch, Kick and YouTube actions.

---

### Core — Platforms

**Action:** `RTS - Action Replay - Core - Platforms`

Handles platform-specific clip creation and capture for Twitch, Kick and YouTube.

This is the platform plumbing used by the Twitch, Kick and YouTube actions.

## A note about commands and aliases

The command names documented here come from the current Streamer.bot action mappings.

That means a couple of oddities are intentional documentation of the current setup rather than an attempt to make the configuration look prettier than it is. If an alias is attached to two actions, both mappings need to be fixed in Streamer.bot before the command can have one unambiguous meaning.

When changing a command, update the Streamer.bot action mapping first and then update this document to match it.
