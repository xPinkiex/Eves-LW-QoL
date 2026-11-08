# Eve's QoL

Quality-of-life features for building in Logic World.

## Features

### Restore Last Selection
Re-selects the last group of components you moved, cloned, rotated, or flipped. Press while building to jump back into multi-selection with those components. Does not remember deleted components.

### Select Line
Selects a line of same-type components. Press while looking at the first component, then press again while looking at the last component. Automatically expands past the second component, in the direction from the first to the second, to grab the full run of evenly-spaced components.

### Freeze / Throttle Wire Updates
At high simulation speeds the client spends a lot of frame time recoloring wires and pegs. The circuit keeps running on the server in every mode.

- **Freeze wire visuals (F7):** wires and pegs stop changing color, but displays and other components keep updating, so you can still watch screens. Unfreezing recolors every changed wire at once.
- **Freeze all circuit visuals (F5):** nothing updates. If the server also runs Eve's QoL (always true in singleplayer), it stops sending circuit state packets entirely while every connected player is fully frozen; the first packet after unfreezing catches everything up. On servers without the mod, or when other players aren't frozen, packets are buffered on your side instead.
- **`EvesQoL.WireUpdateInterval <ms>`** (console): recolor wires at most once every N milliseconds. Displays are unaffected. `0` = every frame, the default.

`EvesQoL.FreezeWires` and `EvesQoL.FreezeAll` toggle the freezes from the console.

## Keybinds

| Key | Action |
|---|---|
| **N** | Restore last selection |
| **M** | Select components in line |
| **F7** | Freeze / unfreeze wire visuals (displays keep updating) |
| **F5** | Freeze / unfreeze all circuit visuals |

All keybinds are rebindable in the controls settings menu.

## Dependencies

- [HarmonyForLogicWorld](https://github.com/Ecconia/HarmonyForLogicWorld)
- EccsLogicWorldAPI