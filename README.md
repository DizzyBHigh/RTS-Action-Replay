# RTS Extension Template

Template repository for The Road to Somewhere Streamer.bot extensions.

## Structure

```text
site/rts.json                         Product and website configuration
RTS Extension - Import Code.txt      Required Streamer.bot import-code source
assets/images/                        Optional extension images
assets/video/                         Optional extension video
overlay/                              Optional OBS browser-source overlay
```

## Create an extension

1. Copy this repository to a new extension repository.
2. Update `site/rts.json` with the real extension information.
3. Build the Streamer.bot extension and add its source code.
4. Export the required Streamer.bot actions and replace the contents of `RTS Extension - Import Code.txt`.
5. Keep the source import-code filename unchanged.
6. Add extension-owned assets under `assets/` and an `overlay/` directory only when needed.
7. Create a version tag and GitHub release.
8. Add the new repository to `products/sources.json` in the RTS website repository when it is ready to be published.

The template repository itself must not be added to the RTS product registry.

## Import-code filename

The source file always keeps the stable name:

`RTS Extension - Import Code.txt`

The release build automatically publishes it using the extension name and release version:

`Extension Name v1.2.3 - Import Code.txt`

The import code is manually maintained. The build changes the published filename only; it does not generate or modify the Streamer.bot import code.

## Publishing

The extension release workflow creates the versioned import-code asset. The RTS website importer consumes the released asset and publishes it with the extension page.

Keep extension-specific website content in `site/rts.json`. Do not edit generated website files.
