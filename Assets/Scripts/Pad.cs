using UnityEngine;

// Gamepad input (Xbox / PS5) through the classic Input Manager. Axes are defined in ProjectSettings/InputManager.asset.
public static class Pad
{
    private static bool Win => Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer;

    private static float Safe(string axis)
    {
        try { return Input.GetAxisRaw(axis); }
        catch (System.ArgumentException) { return 0f; }
    }

    public static float MoveX => Safe("PadLX");
    public static float MoveY => Safe("PadLY");
    public static float LookX => Safe(Win ? "PadRXWin" : "PadRXMac");

    // A / Cross
    public static bool JumpDown => Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.JoystickButton16);
    public static bool JumpHeld => Input.GetKey(KeyCode.JoystickButton0) || Input.GetKey(KeyCode.JoystickButton1) || Input.GetKey(KeyCode.JoystickButton16);

    // Right bumper / R1
    public static bool ButtDown => Input.GetKeyDown(KeyCode.JoystickButton2) || Input.GetKeyDown(KeyCode.JoystickButton3);
    public static bool DashDown => Input.GetKeyDown(KeyCode.JoystickButton5) || Input.GetKeyDown(KeyCode.JoystickButton14);

    // Left bumper / L1
    public static bool DiveHeld => Input.GetKey(KeyCode.JoystickButton4) || Input.GetKey(KeyCode.JoystickButton13);

    // Start / Options
    public static bool StartDown => Input.GetKeyDown(KeyCode.JoystickButton7) || Input.GetKeyDown(KeyCode.JoystickButton9) || Input.GetKeyDown(KeyCode.JoystickButton10);
}
