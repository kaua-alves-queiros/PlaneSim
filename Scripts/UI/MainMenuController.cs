using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PlaneSim.Scripts.Rendering;

namespace PlaneSim.Scripts.UI;

public class MainMenuController
{
    public int SelectedIndex { get; private set; } = 0;
    private const int NumOptions = 3; // 0 = Iniciar Voo, 1 = Ajuda, 2 = Sair

    private float _pulseTimer = 0f;
    private KeyboardState _prevKeyboard;

    public bool ActionStartFlight { get; private set; }
    public bool ActionExitGame { get; private set; }
    public bool ShowHelp { get; private set; } = false;

    public void Reset()
    {
        SelectedIndex = 0;
        ActionStartFlight = false;
        ActionExitGame = false;
        ShowHelp = false;
        _pulseTimer = 0f;
    }

    public void Update(float dt)
    {
        _pulseTimer += dt * 4f;
        KeyboardState kb = Keyboard.GetState();

        if (ShowHelp)
        {
            // Exit help screen on any of these key presses
            if ((kb.IsKeyDown(Keys.Escape) && !_prevKeyboard.IsKeyDown(Keys.Escape)) ||
                (kb.IsKeyDown(Keys.Enter) && !_prevKeyboard.IsKeyDown(Keys.Enter)) ||
                (kb.IsKeyDown(Keys.Space) && !_prevKeyboard.IsKeyDown(Keys.Space)))
            {
                ShowHelp = false;
            }
        }
        else
        {
            // Menu navigation
            if ((kb.IsKeyDown(Keys.Down) && !_prevKeyboard.IsKeyDown(Keys.Down)) ||
                (kb.IsKeyDown(Keys.S) && !_prevKeyboard.IsKeyDown(Keys.S)))
            {
                SelectedIndex = (SelectedIndex + 1) % NumOptions;
            }
            else if ((kb.IsKeyDown(Keys.Up) && !_prevKeyboard.IsKeyDown(Keys.Up)) ||
                     (kb.IsKeyDown(Keys.W) && !_prevKeyboard.IsKeyDown(Keys.W)))
            {
                SelectedIndex = (SelectedIndex - 1 + NumOptions) % NumOptions;
            }

            // Selection confirmation
            if (kb.IsKeyDown(Keys.Enter) && !_prevKeyboard.IsKeyDown(Keys.Enter))
            {
                if (SelectedIndex == 0)
                {
                    ActionStartFlight = true;
                }
                else if (SelectedIndex == 1)
                {
                    ShowHelp = true;
                }
                else if (SelectedIndex == 2)
                {
                    ActionExitGame = true;
                }
            }
        }

        _prevKeyboard = kb;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, int screenWidth, int screenHeight)
    {
        // 1. Draw background sky
        spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, screenWidth, screenHeight), new Color(15, 23, 42)); // Slate dark

        // Draw green terrain landscape background
        int groundHeight = (int)(screenHeight * 0.35f);
        spriteBatch.Draw(pixelTexture, new Rectangle(0, screenHeight - groundHeight, screenWidth, groundHeight), new Color(34, 110, 46));

        // Draw runway strip
        int rx = screenWidth / 2;
        int ry = screenHeight - groundHeight + 40;
        for (int i = 0; i < 20; i++)
        {
            float t = i / 20f;
            int rw = (int)(10 + t * 240);
            int rh = 6;
            spriteBatch.Draw(pixelTexture, new Rectangle(rx - rw / 2, ry + i * 8, rw, rh), new Color(60, 60, 60));
        }

        if (ShowHelp)
        {
            // 2. Render Help Overlay screen
            int panelW = 750;
            int panelH = 500;
            int panelX = (screenWidth - panelW) / 2;
            int panelY = (screenHeight - panelH) / 2;

            // Translucent backing panel
            spriteBatch.Draw(pixelTexture, new Rectangle(panelX, panelY, panelW, panelH), new Color(20, 30, 45, 235));
            // Thin white border line
            spriteBatch.Draw(pixelTexture, new Rectangle(panelX, panelY, panelW, 2), Color.White * 0.4f);
            spriteBatch.Draw(pixelTexture, new Rectangle(panelX, panelY + panelH - 2, panelW, 2), Color.White * 0.4f);
            spriteBatch.Draw(pixelTexture, new Rectangle(panelX, panelY, 2, panelH), Color.White * 0.4f);
            spriteBatch.Draw(pixelTexture, new Rectangle(panelX + panelW - 2, panelY, 2, panelH), Color.White * 0.4f);

            // Title
            PixelFontRenderer.DrawTextCentered(spriteBatch, pixelTexture, "PAINEL DE CONTROLES", screenWidth / 2, panelY + 30, 3, Color.Yellow);

            // SECTION 1: ENGINE (MOTOR)
            int itemX = panelX + 50;
            int section1Y = panelY + 95;
            PixelFontRenderer.DrawText(spriteBatch, pixelTexture, "1. PROPULSAO (ENGINE)", itemX, section1Y, 2, new Color(144, 238, 144));
            PixelFontRenderer.DrawText(spriteBatch, pixelTexture, "TECLA W      -> AUMENTAR POTENCIA (THROTTLE UP)", itemX, section1Y + 30, 1, Color.White);
            PixelFontRenderer.DrawText(spriteBatch, pixelTexture, "TECLA S      -> DIMINUIR POTENCIA (THROTTLE DOWN)", itemX, section1Y + 48, 1, Color.White);

            // SECTION 2: SURFACES (SUPERFICIES DE VOo)
            int section2Y = section1Y + 85;
            PixelFontRenderer.DrawText(spriteBatch, pixelTexture, "2. ATITUDE E MANOBRAS (SURFACES)", itemX, section2Y, 2, new Color(144, 238, 144));
            PixelFontRenderer.DrawText(spriteBatch, pixelTexture, "SETAS C/B    -> MANCHE ARFAGEM (PITCH UP / DOWN)", itemX, section2Y + 30, 1, Color.White);
            PixelFontRenderer.DrawText(spriteBatch, pixelTexture, "SETAS E/D    -> MANCHE ROLAGEM (ROLL LEFT / RIGHT)", itemX, section2Y + 48, 1, Color.White);
            PixelFontRenderer.DrawText(spriteBatch, pixelTexture, "TECLAS A / D -> PEDAIS DE GUINADA (YAW / RUDDER)", itemX, section2Y + 66, 1, Color.White);
            PixelFontRenderer.DrawText(spriteBatch, pixelTexture, "TECLA F      -> ESTAGIOS DOS FLAPS (0* / 15* / 30*)", itemX, section2Y + 84, 1, Color.White);

            // SECTION 3: CAMERA & SIMULATION
            int section3Y = section2Y + 120;
            PixelFontRenderer.DrawText(spriteBatch, pixelTexture, "3. CAMERA E SISTEMA (CAMERA & SIM)", itemX, section3Y, 2, new Color(144, 238, 144));
            PixelFontRenderer.DrawText(spriteBatch, pixelTexture, "TECLA C      -> ALTERNAR CAMERA (3A PESSOA / COCKPIT)", itemX, section3Y + 30, 1, Color.White);
            PixelFontRenderer.DrawText(spriteBatch, pixelTexture, "TECLA R      -> REINICIAR FLIGHT SANDBOX", itemX, section3Y + 48, 1, Color.White);
            PixelFontRenderer.DrawText(spriteBatch, pixelTexture, "TECLA ESC    -> VOLTAR AO MENU / SAIR DO SANDBOX", itemX, section3Y + 66, 1, Color.White);

            // Footer instructions
            int footerY = panelY + panelH - 45;
            float pulseScale = (float)Math.Sin(_pulseTimer * 1.5f) * 0.05f + 1.2f;
            PixelFontRenderer.DrawTextCentered(spriteBatch, pixelTexture, "PRESSIONE ENTER, ESC OU ESPACO PARA VOLTAR", screenWidth / 2, footerY, (int)pulseScale, Color.LightGray);
        }
        else
        {
            // 3. Draw Normal Menu Title and Options
            float scaleMod = (float)Math.Sin(_pulseTimer) * 0.05f + 1f;
            int titleScale = (int)(8 * scaleMod);
            int titleY = (int)(screenHeight * 0.2f);
            
            PixelFontRenderer.DrawTextCentered(spriteBatch, pixelTexture, "PLANESIM", screenWidth / 2 + 4, titleY + 4, titleScale, Color.Black);
            PixelFontRenderer.DrawTextCentered(spriteBatch, pixelTexture, "PLANESIM", screenWidth / 2, titleY, titleScale, Color.White);

            int subtitleY = titleY + 80;
            PixelFontRenderer.DrawTextCentered(spriteBatch, pixelTexture, "SIMULADOR DE VOO 3D", screenWidth / 2, subtitleY, 2, new Color(144, 238, 144));

            int startOptionY = (int)(screenHeight * 0.5f);
            int spacingY = 55;

            for (int i = 0; i < NumOptions; i++)
            {
                string text = i switch
                {
                    0 => "INICIAR VOO",
                    1 => "AJUDA",
                    2 => "SAIR",
                    _ => ""
                };

                bool isSelected = i == SelectedIndex;
                Color color = isSelected ? Color.Yellow : Color.White;
                int scale = isSelected ? 4 : 3;

                if (isSelected)
                {
                    string optionText = $"> {text} <";
                    PixelFontRenderer.DrawTextCentered(spriteBatch, pixelTexture, optionText, screenWidth / 2, startOptionY + i * spacingY, scale, color);
                }
                else
                {
                    PixelFontRenderer.DrawTextCentered(spriteBatch, pixelTexture, text, screenWidth / 2, startOptionY + i * spacingY, scale, color);
                }
            }

            int instY = screenHeight - 60;
            PixelFontRenderer.DrawTextCentered(spriteBatch, pixelTexture, "USE AS SETAS / W-S E PRESSIONE ENTER", screenWidth / 2, instY, 2, Color.LightGray);
            PixelFontRenderer.DrawTextCentered(spriteBatch, pixelTexture, "CONTROLES: SETAS (ARFAGEM/ROLAGEM), W-S (POTENCIA), A-D (LEME), F (FLAPS)", screenWidth / 2, instY + 25, 1, Color.Gray);
        }
    }
}

