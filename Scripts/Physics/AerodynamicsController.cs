using System;
using Microsoft.Xna.Framework;
using PlaneSim.Scripts.Input;

namespace PlaneSim.Scripts.Physics;

public class AerodynamicsController
{
    // Rigid Body State
    public Vector3 Position { get; private set; }
    public Quaternion Rotation { get; private set; } = Quaternion.Identity;
    public Vector3 Velocity { get; private set; }       // World Space (m/s)
    public Vector3 AngularVelocity { get; private set; } // Local Space (rad/s) (X=pitch, Y=yaw, Z=roll)

    // Airplane Constants
    public float Mass { get; } = 1200f; // kg (standard light aircraft weight)
    private readonly Vector3 InertiaTensor = new Vector3(1000f, 1500f, 800f); // X=pitch, Y=yaw, Z=roll inertia

    // Aerodynamic Coefficients
    private const float AirDensitySeaLevel = 1.225f; // kg/m^3
    private const float WingArea = 16.2f;            // m^2
    private const float LiftSlope = 5.0f;            // CL slope per radian
    private const float LiftZeroAoA = 0.15f;         // CL at 0 AoA
    private const float DragParasitic = 0.028f;      // CD0 (parasitic drag)
    private const float InducedDragK = 0.045f;       // CDi factor (K * CL^2)
    private const float StallAngleRad = 0.28f;       // ~16 degrees stall AoA

    // Control Authority (torques scaled by dynamic pressure)
    private const float PitchAuthority = 0.55f;
    private const float RollAuthority = 1.0f;
    private const float YawAuthority = 0.35f;

    // Aerodynamic Damping (keeps the plane stable, prevents endless spinning)
    private const float PitchDamping = 18.0f;
    private const float RollDamping = 22.0f;
    private const float YawDamping = 12.0f;

    // Restoring Torques (longitudinal and directional stability)
    private const float PitchStability = 0.08f;
    private const float YawStability = 0.08f;

    // Side Force Coefficient (air resistance on side profiles of fuselage)
    private const float SideForceCoefficient = 0.45f;

    // Ground Contact Suspension Parameters
    private const float GearHeight = 0.15f;          // Height above Y=0 when on runway
    private const float GroundSpring = 35000f;       // Suspension spring stiffness
    private const float GroundDamp = 6000f;          // Suspension damper
    private const float GroundFrictionSide = 8000f;  // Tire sideways resistance
    private const float GroundRollResist = 150f;     // Rolling resistance

    // Telemetry Outputs
    public float Airspeed { get; private set; } // m/s
    public float AltitudeFeet { get; private set; }
    public float AoADegrees { get; private set; }
    public bool IsStalled { get; private set; }
    public bool IsOnGround { get; private set; }
    public bool HasCrashed { get; private set; }

    public void Reset(Vector3 startPos, Quaternion startRot)
    {
        Position = startPos;
        Rotation = startRot;
        Velocity = Vector3.Zero;
        AngularVelocity = Vector3.Zero;
        Airspeed = 0f;
        AltitudeFeet = startPos.Y * 3.28084f;
        AoADegrees = 0f;
        IsStalled = false;
        IsOnGround = true;
        HasCrashed = false;
    }

    public float GetTerrainHeight(float x, float z)
    {
        float zLimit = 850f; // Flat valley corridor length
        float dz_dist = 0f;
        if (z > zLimit) dz_dist = z - zLimit;
        else if (z < -zLimit) dz_dist = -zLimit - z;

        float distanceToRunway = (float)Math.Sqrt(x * x + dz_dist * dz_dist);
        // Flat valley corridor: damp height to 0 within 450m of the runway line
        float damp = 1.0f - (float)Math.Exp(-(distanceToRunway * distanceToRunway) / (450f * 450f));

        double height = (Math.Sin(x * 0.0015f) * Math.Cos(z * 0.0015f) * 350.0) +
                        (Math.Sin(x * 0.004f + z * 0.003f) * 120.0) +
                        (Math.Cos(x * 0.0004f) * Math.Sin(z * 0.0004f) * 700.0);

        float h = (float)(height * damp);
        return Math.Max(h, -5f);
    }

    public void Update(ControlSurfacesController controls, EngineController engine, float dt)
    {
        if (HasCrashed) return;

        // 1. Calculate Airplane Local Directions
        Vector3 forwardDir = Vector3.Transform(Vector3.Forward, Rotation); // -Z
        Vector3 upDir = Vector3.Transform(Vector3.Up, Rotation);           // +Y
        Vector3 rightDir = Vector3.Transform(Vector3.Right, Rotation);     // +X

        // 2. Air Telemetry
        Airspeed = Velocity.Length();
        AltitudeFeet = Position.Y * 3.28084f;

        // Project velocity onto aircraft axes
        float forwardSpeed = Vector3.Dot(Velocity, forwardDir);
        float rightSpeed = Vector3.Dot(Velocity, rightDir);
        float upSpeed = Vector3.Dot(Velocity, upDir);

        // Angle of Attack (AoA)
        // AoA = atan2(-upSpeed, forwardSpeed)
        float aoa = 0f;
        if (forwardSpeed > 0.1f)
        {
            aoa = (float)Math.Atan2(-upSpeed, forwardSpeed);
        }
        AoADegrees = MathHelper.ToDegrees(aoa);

        // 3. Dynamic Pressure calculation: q = 0.5 * rho * V^2
        float airDensity = AirDensitySeaLevel * Math.Max(0.0f, 1.0f - Position.Y / 10000f);
        float dynamicPressure = 0.5f * airDensity * Airspeed * Airspeed;

        // 4. Calculate Lift and Drag Coefficients
        float cL = 0f;
        IsStalled = Math.Abs(aoa) > StallAngleRad;

        float flapLift = controls.GetFlapLiftOffset();
        float flapDrag = controls.GetFlapDragOffset();

        if (!IsStalled)
        {
            cL = LiftZeroAoA + LiftSlope * aoa + flapLift;
        }
        else
        {
            // Loss of lift after stall angle with smooth drop
            float stallOvershoot = Math.Abs(aoa) - StallAngleRad;
            float stallFactor = Math.Max(0.15f, 1.0f - stallOvershoot * 2.0f);
            cL = (LiftZeroAoA + LiftSlope * StallAngleRad * Math.Sign(aoa)) * stallFactor + flapLift * 0.4f;
        }

        float cD = DragParasitic + InducedDragK * cL * cL + flapDrag;
        if (IsStalled)
        {
            cD += 0.20f; // Massive drag penalty when stalled
        }

        // 5. Calculate Aerodynamic Forces
        Vector3 liftForce = Vector3.Zero;
        Vector3 dragForce = Vector3.Zero;

        if (Airspeed > 0.1f)
        {
            Vector3 velocityNormalized = Vector3.Normalize(Velocity);

            // Lift acts perpendicular to relative wind, banking towards local Up
            Vector3 liftDir = upDir - velocityNormalized * Vector3.Dot(upDir, velocityNormalized);
            if (liftDir.LengthSquared() > 0.001f)
            {
                liftDir.Normalize();
                liftForce = liftDir * (dynamicPressure * WingArea * cL);
            }

            // Drag acts opposite to velocity
            dragForce = -velocityNormalized * (dynamicPressure * WingArea * cD);
        }

        // 6. Thrust Force
        Vector3 thrustForce = forwardDir * engine.Thrust;

        // 7. Gravity
        Vector3 gravityForce = Vector3.Down * Mass * 9.81f;

        // 8. Ground Reaction and Collision Logic
        Vector3 groundForce = Vector3.Zero;
        Vector3 groundTorque = Vector3.Zero;

        float terrainH = GetTerrainHeight(Position.X, Position.Z);
        bool onRunway = Math.Abs(Position.X) < 25f && Math.Abs(Position.Z) < 600f;
        float groundLimitY = onRunway ? GearHeight : terrainH + GearHeight;

        // Contact detection
        if (Position.Y <= groundLimitY + 0.05f)
        {
            IsOnGround = true;

            // CRASH CONDITION
            // Crash if landing outside the runway OR if hitting the ground too hard/unaligned
            if (!onRunway)
            {
                // Mountain or grass crash
                HasCrashed = true;
                return;
            }
            else
            {
                // Runway landing checks
                float pitchAngle = (float)Math.Asin(forwardDir.Y);
                float rollAngle = (float)Math.Asin(rightDir.Y);

                // Crash if landing gear Y velocity is too high, or pitch/roll is skewed
                if (Velocity.Y < -7.0f || Math.Abs(pitchAngle) > 0.45f || Math.Abs(rollAngle) > 0.35f)
                {
                    HasCrashed = true;
                    return;
                }
            }

            // Ground constraints (Suspension)
            float penetration = groundLimitY - Position.Y;
            if (penetration > 0f)
            {
                // Suspension Spring force + Damping force
                float springF = penetration * GroundSpring;
                float dampF = -Velocity.Y * GroundDamp;
                float normalForce = Math.Max(0f, springF + dampF);
                groundForce += Vector3.Up * normalForce;

                // Rolling resistance and lateral wheel friction
                // Lateral friction prevents sliding sideways like on ice
                groundForce += -rightDir * rightSpeed * GroundFrictionSide;
                // Rolling resistance (brakes/friction slowing forward motion)
                groundForce += -forwardDir * forwardSpeed * GroundRollResist;

                // Ground align torques: align aircraft with runway horizon (level pitch and roll)
                float pitchAngle = (float)Math.Asin(forwardDir.Y);
                float rollAngle = (float)Math.Asin(rightDir.Y);

                // Tries to orient nose and wings flat, but allows nose pitching up for takeoff rotation
                float pitchAlignTorque = -pitchAngle * 12000f - AngularVelocity.X * 1800f;
                // If nose gear is down, pull nose to neutral. But if elevator pitch up input is active, allow rotation
                if (controls.PitchDeflection > 0.1f)
                {
                    pitchAlignTorque += controls.PitchDeflection * 8000f;
                }

                float rollAlignTorque = -rollAngle * 25000f - AngularVelocity.Z * 4000f;

                groundTorque = new Vector3(pitchAlignTorque, 0f, rollAlignTorque);
            }
        }
        else
        {
            IsOnGround = false;
        }

        // Torque scales with dynamic pressure, but has a small baseline at low speeds to allow some control
        float dynamicPressureEffective = Math.Max(50f, dynamicPressure);

        // 8.5 Aerodynamic Side Force (opposes sideways sliding velocity)
        Vector3 sideForce = Vector3.Zero;
        if (Airspeed > 0.1f)
        {
            sideForce = -rightDir * rightSpeed * dynamicPressureEffective * SideForceCoefficient;
        }

        // 9. Total Net Force in World Space
        Vector3 netForce = thrustForce + liftForce + dragForce + gravityForce + groundForce + sideForce;

        // 10. Control Torques, Restoring Torques, and Rotational Damping
        Vector3 controlTorque = new Vector3(
            controls.PitchDeflection * PitchAuthority * dynamicPressureEffective,
            controls.YawDeflection * YawAuthority * dynamicPressureEffective,
            controls.RollDeflection * RollAuthority * dynamicPressureEffective
        );

        // Restoring stability: pushes nose to align with relative wind (longitudinal & directional stability)
        Vector3 restoringTorque = new Vector3(
            upSpeed * PitchStability * dynamicPressureEffective,
            -rightSpeed * YawStability * dynamicPressureEffective,
            0f
        );

        // Rotational aerodynamic damping (opposes angular velocity, proportional to speed)
        float speedEffective = Math.Max(2.0f, Airspeed);
        Vector3 dampingTorque = new Vector3(
            -AngularVelocity.X * PitchDamping * speedEffective,
            -AngularVelocity.Y * YawDamping * speedEffective,
            -AngularVelocity.Z * RollDamping * speedEffective
        );

        // Net Torque (Local coordinates)
        Vector3 netTorque = controlTorque + restoringTorque + dampingTorque + groundTorque;

        // 11. Physics Integration (Euler-Cromer)
        // Linear
        Vector3 acceleration = netForce / Mass;
        Velocity += acceleration * dt;
        Position += Velocity * dt;

        // Prevent plane from sinking beneath the ground limit
        if (Position.Y < groundLimitY)
        {
            Position = new Vector3(Position.X, groundLimitY, Position.Z);
            if (Velocity.Y < 0f)
            {
                Velocity = new Vector3(Velocity.X, 0f, Velocity.Z);
            }
        }

        // Angular
        Vector3 angularAcc = netTorque / InertiaTensor;
        AngularVelocity += angularAcc * dt;

        // Clamp local angular velocities to prevent hyper-rotation/twitchy controls
        AngularVelocity = new Vector3(
            Math.Clamp(AngularVelocity.X, -0.9f, 0.9f),  // max pitch rate ~52 deg/s
            Math.Clamp(AngularVelocity.Y, -0.5f, 0.5f),  // max yaw rate ~28 deg/s
            Math.Clamp(AngularVelocity.Z, -1.4f, 1.4f)   // max roll rate ~80 deg/s
        );

        // Integrate Quaternion Rotation
        Vector3 worldAngularVelocity = Vector3.Transform(AngularVelocity, Rotation);
        Quaternion deltaRot = new Quaternion(worldAngularVelocity * 0.5f, 0.0f) * Rotation;
        Rotation = Quaternion.Normalize(new Quaternion(
            Rotation.X + deltaRot.X * dt,
            Rotation.Y + deltaRot.Y * dt,
            Rotation.Z + deltaRot.Z * dt,
            Rotation.W + deltaRot.W * dt
        ));
    }
}
