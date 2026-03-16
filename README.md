# Media Audit

A Playnite extension that scans your game library for missing or non-conforming media (covers, icons, backgrounds, logos, trailers) and tags affected games.

## Features

- **Automatic scanning** -- detects media changes via Playnite events and rescans affected games, with optional interval-based background scanning
- **Configurable standards** -- set expected aspect ratios, resolution limits, and tolerances for each media type
- **Presets** -- built-in presets for common standards (Steam, Epic, GOG, IGDB, SteamGridDB, and more)
- **Tag-based results** -- automatically tags games with issues (e.g. "Undesired Cover", "Missing Trailer") for easy filtering
- **Covers** -- aspect ratio and resolution validation with presets for Steam, Epic, GOG, IGDB, portrait, and landscape formats
- **Icons** -- square aspect ratio and size validation
- **Backgrounds** -- landscape validation with presets from 720p to 4K and ultrawide
- **Logos** -- aspect ratio and resolution checks via ExtraMetadata
- **Trailers and microtrailers** -- existence checks via ExtraMetadata
- **Game music** -- existence checks via PlayniteSound

## Installation

Download the latest `.pext` from [Releases](https://github.com/sharkusmanch/playnite-media-audit/releases) and open it, or install from the Playnite add-on browser.

## Setup

1. Go to **Extensions** > **Media Audit** > **Settings**
2. Choose which media types to check (icons, covers, backgrounds, logos, trailers, game music)
3. Select presets or configure custom aspect ratio and resolution standards
4. Optionally enable interval-based background scanning

Scans run automatically when game media changes. Use **Extensions** > **Media Audit** > **Scan All Game Media** for a full library scan.

## Building

Requires .NET Framework 4.6.2 SDK and [Task](https://taskfile.dev).

```bash
git clone https://github.com/sharkusmanch/playnite-media-audit.git
cd playnite-media-audit
task all
```

## License

[MIT](LICENSE)
