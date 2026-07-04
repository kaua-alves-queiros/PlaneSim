using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlaneSim.Scripts.Core;

namespace PlaneSim;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private GameManager _gameManager;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = false; // Hide mouse during flight simulator for better immersion

        // Set high-definition widescreen resolution suitable for flight simulators
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        
        // Enable depth stencil buffer for 3D rendering depth checks
        _graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
        _graphics.ApplyChanges();
    }

    protected override void Initialize()
    {
        // Instantiate the core Game Manager
        _gameManager = new GameManager(this);
        _gameManager.Initialize();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        // Update game state and controllers
        _gameManager.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Delegate render pass to the game manager
        _gameManager.Draw(_spriteBatch);

        base.Draw(gameTime);
    }
}
