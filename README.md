# Unicorn Pop

A Unity 6 (6000.6.7f1) game. A unicorn stands on top of a castle while a cannon fires balloons into the sky. Pop them with your horn before they float away.

## Play

1. Open this folder as a project in Unity Hub.
2. Open `Assets/Scenes/Main.unity` and press Play.

| Input | Action |
|---|---|
| A / D or arrow keys | Run along the castle |
| Space / W / Up | Jump (press again in the air to double jump) |
| Touch / mouse | Hold to run toward the pointer, quick tap to jump |

- Each escaped balloon costs a heart (3 hearts total); golden balloons are worth 5 and are never penalised.
- Quick consecutive pops build a combo multiplier (up to x8).
- The cannon fires faster the longer you survive. High score is saved locally.

All art and sound are generated in code by `Assets/Scripts/UnicornPopGame.cs`, so there are no extra assets or packages. `Assets/Editor/SceneBuilder.cs` regenerates the scene (menu: Unicorn Pop > Create Main Scene).
