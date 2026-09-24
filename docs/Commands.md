# Action Replay Commands

This is the command reference for RTS Action Replay.

The groups below match the current Streamer.bot action layout. Only actions with chat commands are included here.

The current action list contains 40 chat-command actions across Catalog, Create Clip, Message, Playback and Playlist. Some actions have aliases; all aliases shown below are currently mapped.

---

## RTS - Action Replay - Catalog

Catalog commands work with the current search/result state. Search state is stored per user. To use another user's search, prefix the target with `<platform>:<user>` where supported.

### Add Existing Replay

Add one existing OBS replay file from the configured Replay Folder to the Catalog.

**Command**

`!add-existing-replay "<filename>" <title>`

If no title is supplied, the Replay Title Template is used. Existing Catalog entries are not added again.

### Search By Creator

Search for replays by creator.

**Commands**

`!search-creator <creator> [--amount <N>]`
`!creator-search <creator> [--amount <N>]`

### Search By Date

Search for replays from a date period.

**Commands**

`!search-date <date period> [--amount <N>]`
`!date-search <date period> [--amount <N>]`

Supported period forms depend on the Catalog date parser, including values such as `today`, `this week`, `last week`, `this year`, `last year`, `2025`, `November` and `November 2025`.

### Delete Clip

Delete replay entries from the current Catalog/search result set.

**Command**

`!delete-from-catalog <number(s)>`

Multiple current-page results can be supplied as comma-separated numbers, for example `!delete-from-catalog 1, 2, 3`.

The command removes the Catalog entry and associated Last Played history. It does not delete the physical media file. A replay that is currently playing or still in the Playlist cannot be deleted.

### First Page

Return to the first page of the current Catalog/search results.

**Command**

`!search-first`

This changes the current page without starting a new search.

### Last Page

Jump to the last page of the current Catalog/search results.

**Command**

`!search-last`

### Most Views

List replays ordered by playback count.

**Commands**

`!search-views [--amount <N>]`
`!most-views [--amount <N>]`

Only replays with at least one recorded play are included.

### Next Page

Move to the next page of the current Catalog/search results.

**Command**

`!search-next`

If already on the last page, the current page is unchanged.

### Previous Page

Move to the previous page of the current Catalog/search results.

**Command**

`!search-prev`

If already on the first page, the current page is unchanged.

### Purge

Remove Catalog entries whose replay media can no longer be resolved.

**Command**

`!catalog-purge`

The purge checks local media and stored source URLs where applicable. It removes unavailable Catalog entries only; it does not delete physical files.

### Rate Replay

Rate the replay that is currently playing.

**Commands**

`!rate-replay <rating>`
`!rate-clip <rating>`
`!rate-video <rating>`

`<rating>` must be from 1 to 5. Ratings are stored per user, so rating the same replay again updates that user's rating.

### Search

Search the Catalog by text.

**Command**

`!search <search text> [--amount <N>]`

Running `!search` with no search text returns the Catalog without a text filter.

The optional `--amount` controls how many results are shown per page.

### Show Current Search Page

Show the current page of the current Catalog/search results.

**Command**

`!search-show`

This follows the configured Search Presentation settings. Search results can be sent to the Search Panel, chat, or both.

### Search Top Rated

List replays by rating.

**Command**

`!search-rating [<rating>] [--amount <N>]`

The optional rating accepts 0 to 5.

- `0` = unrated replays.
- `1` = average rating 1.0-1.9.
- `2` = average rating 2.0-2.9.
- `3` = average rating 3.0-3.9.
- `4` = average rating 4.0-4.9.
- `5` = average rating 5.0.

Without a rating, rated replays are returned ordered by average rating.

### Leaderboard - Creator

Show the creator leaderboard.

**Command**

`!creator-leaderboard [<period>] [--amount <N>]`

The optional period can be used to narrow the leaderboard.

### Name Replay

Give a Catalog replay a custom title.

**Commands**

`!name-clip <replay number> <replay title>`
`!rename-clip <replay number> <replay title>`

The replay number refers to the current Catalog order. Custom titles must be unique, ignoring case. Renaming a replay does not rename the physical video file.

### Play History

Play an item from the Last Played history.

**Command**

`!play-history <history item>`

The number is the item's position in the Last Played list.

### List Recent

Show recently captured or added replays.

**Command**

`!search-recent [--amount <N>]`

This uses Catalog recent ordering. It is different from Last Played: a replay can be recent without ever having been played.

### Scan Replay Folder

Scan the configured Replay Folder and add replay files that are not already in the Catalog.

**Command**

`!scan-replay-folder`

The scan only considers files matching the configured Replay File Types.

### Show Replay Leaderboard

Show replay playback activity.

**Command**

`!clip-leaderboard`

---

## RTS - Action Replay - Create Clip

### Create Kick Clip

Request a Kick clip through the KickBot workflow.

**Commands**

`!create-clip [<duration>] [<title>]`
`!createclip [<duration>] [<title>]`

**Kick chat only.**

If no duration is supplied, the configured Kick default is used. The title is everything after the optional duration.

The command asks KickBot to create the clip. Action Replay then captures the returned clip, adds it to the Catalog and queues it in the Playlist.

### Create Twitch Clip

Create a Twitch clip from the current Twitch stream.

**Commands**

`!twitch-clip [<duration>] [<title>]`
`!twitchclip [<duration>] [<title>]`
`!create-clip [<duration>] [<title>]`
`!createclip [<duration>] [<title>]`

**Twitch chat only.**

The allowed duration is 5-60 seconds. If no duration is supplied, the configured Twitch Clip Duration is used.

When created, the clip is added to the Catalog and queued in the Playlist.

### Create YouTube Clip

Create a timestamped clip from the current YouTube live broadcast.

**Commands**

`!create-clip [<duration>] [<title>]`
`!createclip [<duration>] [<title>]`

**YouTube chat only.**

The allowed duration is 5-60 seconds. The title is everything after the optional duration.

Action Replay uses the recorded start time of the current YouTube broadcast to calculate the clip timestamp.

When created, the clip is added to the Catalog and queued in the Playlist.

### Save Replay

Tell OBS to save the current Replay Buffer.

**Commands**

`!savereplay`
`!save-replay`
`!clip-local`
`!cliplocal`

What happens after the save depends on the Replay Source and Playlist settings.

---

## RTS - Action Replay - Message

### Overlay Message

Send a message directly to the RTS overlay.

**Commands**

`!overlay-message <message>`
`!rts-message <message>`

The message uses the configured Message Behaviour, including the selected Design Preset, Branding Preset, Animation Profile and display time.

---

## RTS - Action Replay - Playback

### Playback History

Show the most recently played replays.

**Commands**

`!recently-played [--amount <N>]`
`!last-played [--amount <N>]`

The amount is optional. When omitted, the configured Maximum Recent Clips setting is used.

### Screen Position

Move the player to a saved position.

**Commands**

`!player-pos <position tag> [<duration ms>]`
`!player-position <position tag> [<duration ms>]`

Use the saved Player Position tag, not the display name. For example, the saved position `Full Screen` uses the tag `full-screen`.

The optional duration is the movement time in milliseconds.

### Hide Player

Hide the current player.

**Commands**

`!hide-player`
`!player-hide`

Hiding the player does not delete or reset the replay.

### Pause

Pause the current video.

**Commands**

`!video-pause`
`!player-pause`

### Play

Resume the current video.

**Commands**

`!video-play`
`!player-play`

### Set Speed

Change the current playback speed.

**Commands**

`!set-speed <playback speed>`
`!player-speed <playback speed>`
`!clip-speed <playback speed>`

The command accepts a playback speed from 0.25 to 4.0. Values outside that range are clamped.

### Show Player

Show the current player.

**Commands**

`!show-player`
`!player-show`

Showing the player resumes the current video.

---

## RTS - Action Replay - Playlist

The Playlist is the live playback queue. It is separate from the Catalog and playback history.

### List Playlist

Show the current Playlist.

**Command**

`!playlist`

The list shows the queue in order, including the requester where available.

### Play Replay

Play a replay by adding it to the Playlist.

**Commands**

`!play-clip <replay number>`
`!play-replay <replay number>`

A replay can be selected by Catalog/search result number, custom title, or platform and user search context.

For example:

`!play-replay 4`

`!play-replay Best crash ever`

`!play-replay Twitch:SomeUser 2`

### Playlist - Clear

Remove waiting items from the Playlist while leaving the currently playing item alone.

**Commands**

`!playlist-clear`
`!playlistclear`

### Playlist - Clear All

Remove everything from the Playlist.

**Commands**

`!playlist-clearall`
`!playlistclearall`

This does not stop a replay that is already playing.

### Playlist - Pause

Pause automatic Playlist progression.

**Commands**

`!playlist-pause`
`!playlistpause`

Pause does not stop the current video.

### Playlist - Remove

Remove a waiting item by its Playlist position.

**Commands**

`!playlistremove <playlist item>`
`!playlist-remove <playlist item>`

The currently playing item cannot be removed.

### Playlist - Resume

Resume Playlist progression.

**Commands**

`!playlistresume`
`!playlist-resume`

If nothing is playing and queued items exist, Resume starts the next item.

---

## Command aliases and action mappings

The command names in this document are taken from the current Streamer.bot action mappings.

When changing a command in Streamer.bot, update `streamerbot/SB-Mappings.txt` and this document together.
