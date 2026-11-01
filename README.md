# Unicorn Pop

A Unity 6 (6000.6.7f1) 3D game, built in small testable batches. A winged unicorn flies around a meadow in front of a distant castle, where a jester loads a cannon that fires balloons. Pop them with your horn.

## Play

1. Open this folder as a project in Unity Hub.
2. Open `Assets/Scenes/Main.unity` and press Play.

| Input | Action |
|---|---|
| W / S or Up / Down | Move forward / back |
| A / D or Left / Right | Turn |
| Space | Jump, then flap in the air (hold to glide) |
| Shift | Dash |
| Ctrl or C | Dive |

**Gamepad (Xbox / PS5):** left stick walks and strafes, right stick turns, A/Cross jumps and flaps, right bumper dashes, left bumper dives, Start restarts.

Flaps use stamina (blue bar); the dash cooldown is the yellow bar.

## Progress

- [x] Batch 1: 3D world, castle, jester, cannon, flying unicorn with chase camera
- [x] Batch 2: jester loads the cannon, balloons fly out
- [x] Batch 3: horn popping, score, combo, golden balloons, hearts
- [x] Batch 4: title screen, saved high score, sound, fireworks
- [x] Batch 5: difficulty ramp (levels) and a cheering king
- [x] Batch 6: dreamland scenery (gradient sky, sun, rainbow, floating islands, pond, flowers, winding path)
- [x] Batch 7: living world (butterflies, fireflies, birds, castle banners, glowing windows, drawbridge)
- [x] Batch 8: new balloons (rainbow, heart, star, storm)
- [x] Batch 9: power-ups and a giant boss balloon
- [x] Batch 10: level themes (sunset, night, candy clouds, aurora) with level banners
- [ ] Batch 11: dreamy procedural music and ambience
- [ ] Batch 12: game feel (screen shake, slow-mo, speed lines, hoofprints, pastel HUD)

All models are built from Unity primitives in code, so there are no extra assets or packages. `Assets/Editor/SceneBuilder.cs` regenerates the scene (menu: Unicorn Pop > Create Main Scene).
