# Roblox Studio Mod Manager for Linux

A Linux-native port of [Roblox Studio Mod Manager](https://github.com/MaximumADHD/Roblox-Studio-Mod-Manager), initially targeting Roblox Studio running through [Vinegar](https://github.com/vinegarhq/vinegar).

## Status

**Early development.** The current branch contains the first Linux/Vinegar integration layer. It is not feature-complete yet.

### Current goals

- Detect the Vinegar Flatpak (`org.vinegarhq.Vinegar`)
- Discover Vinegar's Linux data directory and Studio prefix
- Locate `RobloxStudioBeta.exe` inside the prefix
- Launch Studio through Vinegar
- Replace Windows Registry integration with Linux desktop/protocol integration
- Port the original FFlag, package, version, and Studio management functionality
- Eventually provide a native GUI

## Development

Requirements:

- Linux x86_64
- .NET 8 SDK
- Flatpak
- Vinegar (`org.vinegarhq.Vinegar`)

Build:

```bash
dotnet build Roblox-Studio-Mod-Manager-Linux.sln
```

Run:

```bash
dotnet run --project src/RobloxStudioModManager.Linux
```

Launch through Vinegar:

```bash
dotnet run --project src/RobloxStudioModManager.Linux -- launch
```

## Design

The port intentionally does not depend on a native `vinegar` executable being present in `$PATH`. It talks to the Flatpak installation through its app ID and discovers user data using normal XDG/Flatpak paths.

Windows-specific functionality from the original project is being moved behind platform abstractions instead of being copied into the Linux implementation.

## Attribution

This project is an independent Linux port/reimplementation inspired by MaximumADHD's Roblox Studio Mod Manager. The original project is available at https://github.com/MaximumADHD/Roblox-Studio-Mod-Manager.
