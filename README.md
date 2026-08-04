# StarCountUnlocker

Unlocks the **star count slider** in Dyson Sphere Program's new game creation screen, allowing you to select up to **1024 stars** (vanilla default max is 256).

## Features

- Expands the star count slider range: **1 ~ 1024** stars (configurable, default 1~1024)
- Automatically patches internal array sizes (`GalaxyData.astrosData`, `SectorModel` arrays) to support larger galaxies
- DLC compatible — automatically detects and patches DLC-specific constants
- Config file generated at `BepInEx/config/ywscjlq.star.count.unlocker.cfg`

## Installation

1. Install [BepInEx](https://thunderstore.io/c/dyson-sphere-program/p/xiaoye97/BepInEx/) for DSP
2. Extract this mod into `BepInEx/plugins/`
3. Launch the game, go to **New Game**, and enjoy the expanded star count slider!

## Configuration

After running the game once, edit:
```
BepInEx/config/ywscjlq.star.count.unlocker.cfg
```

| Setting | Default | Description |
|---------|---------|-------------|
| `MaxStars` | 1024 | Maximum stars on the slider |
| `MinStars` | 1 | Minimum stars on the slider |

## Compatibility

- **DLC compatible** ✓ — auto-detects DLC-specific array constants
- **Nebula/Multiplayer** — should work (BepInEx plugin, Harmony patcher)

## Changelog

### 1.0.0
- Initial release
- Slider range expansion (64~1024 stars)
- ASTRO_COUNT constant patching via Harmony transpiler
- DLC constant auto-detection
