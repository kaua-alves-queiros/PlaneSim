using System;
using Microsoft.Xna.Framework;
using PlaneSim.Scripts.Input;
using PlaneSim.Scripts.Physics;

namespace PlaneSim.Scripts.Camera;

public class CameraController
{
    public enum CameraMode
    {
        ThirdPerson,
        Cockpit
    }

    public CameraMode Mode { get; private set; } = CameraMode.ThirdPerson;

    public Matrix ViewMatrix { get; private set; }
    public Matrix ProjectionMatrix { get; private set; }
    public Vector3 Position { get; private set; }

    // Follow settings
    private const float FollowDistance = 8.5f;
    private const float FollowHeight = 1.8f;
    private const float CamLerpSpeed = 7.0f;    // Smooth damp speed for position
    private const float UpLerpSpeed = 4.5f;     // Smooth damp speed for roll/banking

    private Vector3 _smoothCamPos;
    private Vector3 _smoothUp = Vector3.Up;
    private bool _isFirstFrame = true;

    public void Reset()
    {
        _isFirstFrame = true;
    }

    public void Update(AerodynamicsController plane, FlightInputActions input, float aspectRatio, float dt)
    {
        // 1. Toggle mode on input
        if (input.ToggleCamera)
        {
            Mode = Mode == CameraMode.ThirdPerson ? CameraMode.Cockpit : CameraMode.ThirdPerson;
        }

        // 2. Fetch plane vectors
        Vector3 planePos = plane.Position;
        Vector3 forward = Vector3.Transform(Vector3.Forward, plane.Rotation); // -Z (Forward)
        Vector3 up = Vector3.Transform(Vector3.Up, plane.Rotation);           // +Y (Up)

        // 3. Setup Projection Matrix
        ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(Mode == CameraMode.Cockpit ? 65f : 60f), // Slightly wider FOV in cockpit
            aspectRatio,
            0.1f,
            12000f
        );

        // 4. Calculate camera vectors based on mode
        if (Mode == CameraMode.ThirdPerson)
        {
            // Ideal third-person position behind and slightly above the aircraft
            Vector3 idealCamPos = planePos - forward * FollowDistance + up * FollowHeight;

            if (_isFirstFrame)
            {
                _smoothCamPos = idealCamPos;
                _smoothUp = up;
                _isFirstFrame = false;
            }
            else
            {
                // Smooth follow using Lerp
                _smoothCamPos = Vector3.Lerp(_smoothCamPos, idealCamPos, dt * CamLerpSpeed);
                _smoothUp = Vector3.Normalize(Vector3.Lerp(_smoothUp, up, dt * UpLerpSpeed));
            }

            Position = _smoothCamPos;
            Vector3 lookTarget = planePos + forward * 1.5f; // Look slightly ahead of the plane center
            ViewMatrix = Matrix.CreateLookAt(Position, lookTarget, _smoothUp);
        }
        else // Cockpit Mode
        {
            _isFirstFrame = true; // reset third person smoothing for when we toggle back

            // Place camera inside the cockpit canopy
            Vector3 cockpitOffset = forward * 0.35f + up * 0.3f;
            Position = planePos + cockpitOffset;

            Vector3 lookTarget = Position + forward * 10f; // Look straight ahead along aircraft nose
            ViewMatrix = Matrix.CreateLookAt(Position, lookTarget, up);
        }
    }
}
