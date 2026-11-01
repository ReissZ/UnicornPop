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

## Story

Dreamland runs on happy wishes. Every wish becomes a balloon, and a balloon is meant to float up and pop into sparkles, which keeps the rainbow lit, the fireflies glowing, the islands floating and the sky changing colour.

- **The unicorn (Pip):** the kingdom's last Wish-Popper. Her golden horn is the only thing that can pop a wish without hurting it, so the dream inside is released and saved. She is brave, a bit clumsy and easily shocked. Her rainbow dash is old magic.
- **The king (King Bartholomew the Worried):** a good ruler who fusses. A wish that escapes into the sky takes some of Dreamland's magic with it, so he stomps when one gets away. He is secretly lonely, and the grimacing grin is him trying not to look delighted.
- **The jester (Jingles):** the royal Wish-Launcher. His cannon sends wishes up so they can be popped. He is not the villain, just an over-enthusiastic coach who speeds up when you do well.
- **The balloons:** normal balloons are unpopped wishes. Hearts are the kingdom's courage, gold and star balloons are rare wishes, storm balloons are wishes gone sour, and the crowned boss is a cranky wish nobody has made in a hundred years.
- **Possible twist:** the boss holds the very first wish that made Dreamland, and the king made it because he wished for a friend.

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
- [x] Batch 11: dreamy procedural music and ambience
- [x] Batch 12: game feel (screen shake, slow-mo, speed lines, hoofprints, pastel HUD)
- [x] Title screen cinematic: loop-the-loop flip and landing, shocked-eyebrows close-up, then quick zooms to the grinning king and the cackling jester (starts the game at any time)

All models are built from Unity primitives in code, so there are no extra assets or packages. `Assets/Editor/SceneBuilder.cs` regenerates the scene (menu: Unicorn Pop > Create Main Scene).
