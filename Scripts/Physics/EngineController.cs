using System;
using PlaneSim.Scripts.Input;

namespace PlaneSim.Scripts.Physics;

public class EngineController
{
    public float Throttle { get; private set; } = 0.0f; // 0.0 to 1.0
    public float Thrust { get; private set; } = 0.0f;    // Newtons

    private const float ThrottleAdjustSpeed = 0.4f; // 40% change per second
    private const float MaxThrustForce = 8500f;     // Maximum engine thrust in Newtons (tuned for a ~1500kg airplane)

    public void Reset()
    {
        Throttle = 0.0f;
        Thrust = 0.0f;
    }

    public void Update(FlightInputActions input, float dt)
    {
        // Adjust throttle based on input
        if (input.ThrottleChange != 0f)
        {
            Throttle = Math.Clamp(Throttle + input.ThrottleChange * ThrottleAdjustSpeed * dt, 0.0f, 1.0f);
        }

        // Calculate current thrust
        Thrust = Throttle * MaxThrustForce;
    }
}
