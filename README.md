# Player.NET

[![Build](https://github.com/guypritchard/Player.NET/actions/workflows/build.yml/badge.svg)](https://github.com/guypritchard/Player.NET/actions/workflows/build.yml)

Player.NET is a compact, artwork-first Windows music player. The current player uses a code-first Avalonia UI on .NET 10 while preserving the original 400x400 chromeless design and playback engine.

## Features

- Local MP3, WAV, and WMA playback.
- Dynamic directory playlists: opening one track loads the supported files beside it and starts at the selected track.
- Embedded metadata and artwork, with directory artwork fallback in the player and a neutral placeholder in playlists.
- Docked artwork playlist with active-track highlighting and direct playback.
- Gapless 250 ms blending when moving to the previous or next compatible track.
- Main, minimalist, and 400x50 mini modes; double-click empty player space to cycle modes.
- Album-reactive visualizations including oscilloscopes, FFT bars, waterfall spectrogram, circular frequency scope, Pixel Mosaic, and Liquid Artwork.
- Windows 11 taskbar album-art overlay, play/pause state, and playback progress.
- Random, repeat, seeking, media controls, and persisted player state.

## Requirements

- Windows 10 or Windows 11 for local development.
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

Published `win-x64` release archives are self-contained and do not require a separately installed .NET runtime.

## Run

```powershell
dotnet run --project Player.Avalonia
```

## Controls

- Click the center control to play or pause.
- Use the side controls for previous and next.
- Drag the bottom progress line to seek without pausing playback.
- Click the settings control to cycle visualizations.
- Click the playlist control to show or hide the docked playlist.
- Double-click empty player space to cycle `main -> minimal -> mini -> main`.
- In minimal mode, hover over the player to reveal controls and metadata.

## Build And Test

```powershell
dotnet build DJPad.sln --configuration Release -warnaserror
dotnet test DJPad.Tests/DJPad.Tests.csproj --configuration Release --filter "TestCategory!=Integration"
```

The optional real-audio lifecycle test requires a local playable file:

```powershell
$env:DJPAD_SMOKE_FILE = 'C:\Music\sample.mp3'
dotnet test DJPad.Tests/DJPad.Tests.csproj --configuration Release --filter "TestCategory=Integration"
```

## Releases

GitVersion calculates semantic versions from the full Git history. Every CI run publishes a versioned, self-contained `win-x64` workflow artifact. Pushing a stable tag such as `v0.1.0` also creates a GitHub release containing:

```text
Player.NET-0.1.0-win-x64.zip
```

Release tags must use `vMAJOR.MINOR.PATCH` and point to a commit contained in `master`.

```powershell
git tag -a v0.1.0 -m "Player.NET v0.1.0"
git push origin v0.1.0
```

Commit messages can direct the next GitVersion increment with `+semver: major`, `+semver: minor`, `+semver: patch`, or `+semver: none`.
