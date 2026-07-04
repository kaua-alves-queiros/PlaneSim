using System;
using PlaneSim.Scripts.Input;

namespace PlaneSim.Scripts.Physics;

public class ControlSurfacesController
{
    // Flap states: 0 = 0°, 1 = 15°, 2 = 30°
    public int FlapStage { get; private set; } = 0;
    
    // Smoothly interpolated control deflections (-1.0 to 1.0)
    public float PitchDeflection { get; private set; } = 0.0f;
    public float RollDeflection { get; private set; } = 0.0f;
    public float YawDeflection { get; private set; } = 0.0f;

    private const float ActuatorSpeed = 5.0f; // Speed at which surfaces move to target (rad/sec equivalent)

    public void Reset()
    {
        FlapStage = 0;
        PitchDeflection = 0f;
        RollDeflection = 0f;
        YawDeflection = 0f;
    }

    public float GetFlapAngleDegrees()
    {
        return FlapStage switch
        {
            1 => 15f,
            2 => 30f,
            _ => 0f
        };
    }

    public float GetFlapLiftOffset()
    {
        // Flaps increase lift coefficient to allow flying at lower speeds
        return FlapStage switch
        {
            1 => 0.10f,
            2 => 0.20f,
            _ => 0.0f
        };
    }

    public float GetFlapDragOffset()
    {
        // Flaps increase drag coefficient, helping to slow down for landing
        return FlapStage switch
        {
            1 => 0.08f,
            2 => 0.22f,
            _ => 0.0f
        };
    }

    public void Update(FlightInputActions input, float dt)
    {
        // Cycle flap stages on toggle input (0 -> 1 -> 2 -> 0)
        if (input.ToggleFlaps)
        {
            FlapStage = (FlapStage + 1) % 3;
        }

        // Smoothly interpolate control deflections towards inputs to simulate mechanical lag
        PitchDeflection = Math.Clamp(PitchDeflection + (input.Pitch - PitchDeflection) * ActuatorSpeed * dt, -1f, 1f);
        RollDeflection = Math.Clamp(RollDeflection + (input.Roll - RollDeflection) * ActuatorSpeed * dt, -1f, 1f);
        YawDeflection = Math.Clamp(YawDeflection + (input.Yaw - YawDeflection) * ActuatorSpeed * dt, -1f, 1f);
    }
}
