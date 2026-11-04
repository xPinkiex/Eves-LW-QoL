# Eve's QoL

Quality-of-life features for building in Logic World.

## Features

### Restore Last Selection
Re-selects the last group of components you moved, cloned, rotated, or flipped. Press while building to jump back into multi-selection with those components. Does not remember deleted components.

### Select Line
Selects a line of same-type components. Press while looking at the first component, then press again while looking at the last component. Automatically expands past both anchors to grab the full run of evenly-spaced components.

### Freeze / Throttle Wire Updates
At high simulation speeds the client spends a lot of frame time recoloring wires and pegs and updating components. Freezing stops all of that while the circuit keeps running on the server; unfreezing applies the latest state of every changed wire at once.

If the server also runs Eve's QoL (always true in singleplayer), freezing tells the server to stop sending circuit state packets entirely while every connected player is frozen. The server keeps track of what changed, so the first packet after unfreezing catches everything up. On servers without the mod, or when other players aren't frozen, packets still arrive and are buffered on your side instead.

For a middle ground, `EvesQoL.WireUpdateInterval <ms>` in the console applies updates at most once every N milliseconds (`0` = every frame, the default). `EvesQoL.FreezeWires` toggles freezing from the console.

## Keybinds

| Key | Action |
|---|---|
| **N** | Restore last selection |
| **M** | Select components in line |
| **F7** | Freeze / unfreeze wire updates |

All keybinds are rebindable in the controls settings menu.

## Dependencies

- [HarmonyForLogicWorld](https://github.com/Ecconia/HarmonyForLogicWorld)
- EccsLogicWorldAPI