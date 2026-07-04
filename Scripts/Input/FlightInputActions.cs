using Microsoft.Xna.Framework.Input;

namespace PlaneSim.Scripts.Input;

public class FlightInputActions
{
    // Continuous Inputs (-1.0 to 1.0)
    public float Pitch { get; private set; }   // Negative = Nose Down, Positive = Nose Up
    public float Roll { get; private set; }    // Negative = Left, Positive = Right
    public float Yaw { get; private set; }     // Negative = Left, Positive = Right
    public float ThrottleChange { get; private set; } // Negative = Throttle Down, Positive = Throttle Up

    // Discrete Toggles (Pressed in current frame but not in previous)
    public bool ToggleFlaps { get; private set; }
    public bool ToggleCamera { get; private set; }
    public bool TriggerReset { get; private set; }
    public bool TriggerExit { get; private set; }

    private KeyboardState _prevKeyboardState;

    public void Update()
    {
        KeyboardState kb = Keyboard.GetState();

        // 1. Throttle changes: W to increase, S to decrease
        ThrottleChange = 0f;
        if (kb.IsKeyDown(Keys.W)) ThrottleChange += 1f;
        if (kb.IsKeyDown(Keys.S)) ThrottleChange -= 1f;

        // 2. Pitch: Up Arrow = Pitch Down (push stick forward), Down Arrow = Pitch Up (pull stick back)
        Pitch = 0f;
        if (kb.IsKeyDown(Keys.Down)) Pitch += 1f;
        if (kb.IsKeyDown(Keys.Up)) Pitch -= 1f;

        // 3. Roll: Left Arrow = Roll Left, Right Arrow = Roll Right (ailerons)
        Roll = 0f;
        if (kb.IsKeyDown(Keys.Left)) Roll += 1f;
        if (kb.IsKeyDown(Keys.Right)) Roll -= 1f;

        // 4. Yaw: A = Yaw Left, D = Yaw Right (rudder/pedals)
        Yaw = 0f;
        if (kb.IsKeyDown(Keys.A)) Yaw += 1f;
        if (kb.IsKeyDown(Keys.D)) Yaw -= 1f;

        // 5. Single-press Toggles
        ToggleFlaps = kb.IsKeyDown(Keys.F) && !_prevKeyboardState.IsKeyDown(Keys.F);
        ToggleCamera = kb.IsKeyDown(Keys.C) && !_prevKeyboardState.IsKeyDown(Keys.C);
        TriggerReset = kb.IsKeyDown(Keys.R) && !_prevKeyboardState.IsKeyDown(Keys.R);
        TriggerExit = kb.IsKeyDown(Keys.Escape) && !_prevKeyboardState.IsKeyDown(Keys.Escape);

        _prevKeyboardState = kb;
    }
}
