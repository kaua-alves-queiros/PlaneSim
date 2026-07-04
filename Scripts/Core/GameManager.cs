using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PlaneSim.Scripts.Input;
using PlaneSim.Scripts.Physics;
using PlaneSim.Scripts.Camera;
using PlaneSim.Scripts.UI;
using PlaneSim.Scripts.Rendering;

namespace PlaneSim.Scripts.Core;

public enum GameState
{
    MainMenu,
    FlightSandbox
}

public class GameManager
{
    private readonly Game _game;
    private readonly GraphicsDevice _graphicsDevice;

    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    // Sub-Systems
    private readonly FlightInputActions _input = new();
    private readonly EngineController _engine = new();
    private readonly ControlSurfacesController _controls = new();
    private readonly AerodynamicsController _planePhysics = new();
    private readonly CameraController _camera = new();
    private readonly MainMenuController _menu = new();
    private readonly FlightHUDController _hud = new();

    // 3D Rendering Assets (Procedural)
    private VertexPositionNormalColor[] _terrainVertices;
    private VertexPositionNormalColor[] _runwayVertices;
    private BasicEffect _basicEffect;

    // UI Texture
    private Texture2D _pixelTexture;

    // Flight variables
    private float _propAngle = 0f;
    private bool _hasFlown = false;
    private bool _isLandedSuccess = false;

    public GameManager(Game game)
    {
        _game = game;
        _graphicsDevice = game.GraphicsDevice;
    }

    public void Initialize()
    {
        // Setup 3D shader (BasicEffect)
        _basicEffect = new BasicEffect(_graphicsDevice)
        {
            LightingEnabled = true,
            VertexColorEnabled = true
        };

        // Directional Light 0 (Warm sun light)
        _basicEffect.DirectionalLight0.Enabled = true;
        _basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(1f, -1.8f, -1f));
        _basicEffect.DirectionalLight0.DiffuseColor = new Vector3(1.0f, 0.98f, 0.92f);
        _basicEffect.DirectionalLight0.SpecularColor = new Vector3(0.5f, 0.5f, 0.5f);

        // Directional Light 1 (Cool sky dome fill light)
        _basicEffect.DirectionalLight1.Enabled = true;
        _basicEffect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(-1f, 1f, 1f));
        _basicEffect.DirectionalLight1.DiffuseColor = new Vector3(0.2f, 0.25f, 0.35f);

        _basicEffect.AmbientLightColor = new Vector3(0.35f, 0.38f, 0.45f);

        // Generate procedural environment geometry
        // Terrain: 90x90 grid covering 11,000 x 11,000 meters
        _terrainVertices = MeshBuilder.CreateTerrain(95, 95, 11000f, 11000f);
        _runwayVertices = MeshBuilder.CreateRunway();

        // Create 1x1 white texture for HUD/UI drawing
        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        // Initialize state
        ResetFlight();
        _menu.Reset();
    }

    private void ResetFlight()
    {
        // Runway starts at Z = 500f, facing north (-Z direction, which is Rotation.Identity)
        Vector3 startPos = new Vector3(0f, 0.15f, 500f);
        _planePhysics.Reset(startPos, Quaternion.Identity);
        _engine.Reset();
        _controls.Reset();
        _camera.Reset();
        _propAngle = 0f;
        _hasFlown = false;
        _isLandedSuccess = false;
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        switch (CurrentState)
        {
            case GameState.MainMenu:
                _menu.Update(dt);
                if (_menu.ActionStartFlight)
                {
                    ResetFlight();
                    CurrentState = GameState.FlightSandbox;
                }
                else if (_menu.ActionExitGame)
                {
                    _game.Exit();
                }
                break;

            case GameState.FlightSandbox:
                _input.Update();

                // Exit to menu
                if (_input.TriggerExit)
                {
                    _menu.Reset();
                    CurrentState = GameState.MainMenu;
                    break;
                }

                // Reset flight Sandbox
                if (_input.TriggerReset)
                {
                    ResetFlight();
                }

                if (!_planePhysics.HasCrashed && !_isLandedSuccess)
                {
                    // Update flight physics
                    _engine.Update(_input, dt);
                    _controls.Update(_input, dt);
                    _planePhysics.Update(_controls, _engine, dt);

                    // Rotate propeller based on engine power
                    float speedFactor = 0.1f + _engine.Throttle * 0.9f;
                    _propAngle = (_propAngle + 55f * speedFactor * dt) % MathHelper.TwoPi;

                    // Detect takeoff (exceeded 30 meters altitude)
                    if (_planePhysics.Position.Y > 30f)
                    {
                        _hasFlown = true;
                    }

                    // Detect successful landing
                    // Must have flown, be on ground, throttle cut to 0%, and decelerated to a complete stop
                    if (_hasFlown && _planePhysics.IsOnGround && 
                        _planePhysics.Airspeed < 0.25f && _engine.Throttle <= 0.01f)
                    {
                        _isLandedSuccess = true;
                    }
                }
                else
                {
                    // If crashed or landed, engine/controls cut out
                    _engine.Reset();
                }

                // Update Camera to follow aircraft
                float aspect = (float)_graphicsDevice.Viewport.Width / _graphicsDevice.Viewport.Height;
                _camera.Update(_planePhysics, _input, aspect, dt);
                _hud.Update(dt);
                break;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // 1. Render 3D Scene (Skybox color + Terrain, Runway, and Airplane)
        _graphicsDevice.DepthStencilState = DepthStencilState.Default;
        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.RasterizerState = RasterizerState.CullNone;

        if (CurrentState == GameState.FlightSandbox)
        {
            // Clear screen to soft sky-blue
            _graphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, new Color(135, 206, 235), 1.0f, 0);

            // Bind Camera Matrix properties to Shader
            _basicEffect.View = _camera.ViewMatrix;
            _basicEffect.Projection = _camera.ProjectionMatrix;

            // Draw Terrain
            _basicEffect.World = Matrix.Identity;
            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, _terrainVertices, 0, _terrainVertices.Length / 3);
            }

            // Draw Runway
            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, _runwayVertices, 0, _runwayVertices.Length / 3);
            }

            // Generate Airplane vertices on-the-fly to animate the propeller rotation
            var planeVertices = MeshBuilder.CreateAirplane(_propAngle);

            // Position and Rotate airplane mesh in world coordinates
            _basicEffect.World = Matrix.CreateFromQuaternion(_planePhysics.Rotation) * Matrix.CreateTranslation(_planePhysics.Position);
            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, planeVertices, 0, planeVertices.Length / 3);
            }
        }

        // 2. Render 2D UI Overlay (Menu or HUD)
        // Set point sampler to draw text sharp, disable depth testing for UI overlay
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);

        int sw = _graphicsDevice.Viewport.Width;
        int sh = _graphicsDevice.Viewport.Height;

        if (CurrentState == GameState.MainMenu)
        {
            _menu.Draw(spriteBatch, _pixelTexture, sw, sh);
        }
        else if (CurrentState == GameState.FlightSandbox)
        {
            _hud.Draw(spriteBatch, _pixelTexture, _planePhysics, _controls, _engine, _camera, sw, sh, _isLandedSuccess);
        }

        spriteBatch.End();
    }
}
