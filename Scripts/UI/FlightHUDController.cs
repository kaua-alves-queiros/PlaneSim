using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlaneSim.Scripts.Rendering;
using PlaneSim.Scripts.Physics;
using PlaneSim.Scripts.Camera;

namespace PlaneSim.Scripts.UI;

public class FlightHUDController
{
    private float _flashTimer = 0f;

    public void Update(float dt)
    {
        _flashTimer += dt * 5f;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, AerodynamicsController plane, 
        ControlSurfacesController controls, EngineController engine, CameraController camera,
        int screenWidth, int screenHeight, bool isLandedSuccess)
    {
        // 1. Draw Flight Director Crosshair (only in cockpit view)
        if (camera.Mode == CameraController.CameraMode.Cockpit)
        {
            DrawCrosshair(spriteBatch, pixelTexture, screenWidth / 2, screenHeight / 2);
        }

        // 2. Draw Left Telemetry Panel (Airspeed)
        DrawGlassPanel(spriteBatch, pixelTexture, 15, 15, 230, 95);
        int kts = (int)(plane.Airspeed * 1.94384f);
        int kmh = (int)(plane.Airspeed * 3.6f);
        PixelFontRenderer.DrawText(spriteBatch, pixelTexture, "VELOCIDADE (AIRSPEED)", 25, 25, 1, Color.LightGray);
        PixelFontRenderer.DrawText(spriteBatch, pixelTexture, $"{kts} KNOTS", 25, 45, 3, Color.White);
        PixelFontRenderer.DrawText(spriteBatch, pixelTexture, $"{kmh} KM/H", 25, 80, 1, Color.DarkGray);

        // 3. Draw Right Telemetry Panel (Altitude)
        int panelW = 230;
        DrawGlassPanel(spriteBatch, pixelTexture, screenWidth - panelW - 15, 15, panelW, 95);
        int altFeet = (int)plane.AltitudeFeet;
        int altMeters = (int)(plane.Position.Y);
        PixelFontRenderer.DrawText(spriteBatch, pixelTexture, "ALTITUDE", screenWidth - panelW + 5, 25, 1, Color.LightGray);
        PixelFontRenderer.DrawText(spriteBatch, pixelTexture, $"{altFeet} FT", screenWidth - panelW + 5, 45, 3, Color.White);
        PixelFontRenderer.DrawText(spriteBatch, pixelTexture, $"{altMeters} METROS", screenWidth - panelW + 5, 80, 1, Color.DarkGray);

        // 4. Draw Bottom Configuration Panel (Engine Throttle & Flaps & AoA)
        int botPanelW = 460;
        int botPanelH = 65;
        int botPanelX = (screenWidth - botPanelW) / 2;
        int botPanelY = screenHeight - botPanelH - 15;
        DrawGlassPanel(spriteBatch, pixelTexture, botPanelX, botPanelY, botPanelW, botPanelH);

        // Throttle Gauge
        int throttlePercent = (int)(engine.Throttle * 100f);
        PixelFontRenderer.DrawText(spriteBatch, pixelTexture, "MOTOR (THROTTLE)", botPanelX + 15, botPanelY + 15, 1, Color.LightGray);
        PixelFontRenderer.DrawText(spriteBatch, pixelTexture, $"{throttlePercent}%", botPanelX + 15, botPanelY + 32, 2, Color.White);

        // Flaps Indicator
        string flapsText = controls.FlapStage switch
        {
            1 => "15*",
            2 => "30*",
            _ => "0*"
        };
        PixelFontRenderer.DrawText(spriteBatch, pixelTexture, "FLAPS", botPanelX + 180, botPanelY + 15, 1, Color.LightGray);
        PixelFontRenderer.DrawText(spriteBatch, pixelTexture, $"[ {flapsText} ]", botPanelX + 180, botPanelY + 32, 2, Color.White);

        // Angle of Attack (AoA)
        int aoa = (int)plane.AoADegrees;
        Color aoaColor = Math.Abs(aoa) > 12 ? Color.Orange : Color.White;
        PixelFontRenderer.DrawText(spriteBatch, pixelTexture, "ANGULO ATAQUE (AOA)", botPanelX + 290, botPanelY + 15, 1, Color.LightGray);
        PixelFontRenderer.DrawText(spriteBatch, pixelTexture, $"{aoa}*", botPanelX + 290, botPanelY + 32, 2, aoaColor);

        // 5. Stall Alert (Estol) - Flashing Red Banner
        if (plane.IsStalled && !plane.IsOnGround)
        {
            bool isFlashOn = ((int)_flashTimer % 2) == 0;
            if (isFlashOn)
            {
                int alertW = 340;
                int alertH = 45;
                int alertX = (screenWidth - alertW) / 2;
                int alertY = 130;

                // Red outline box
                spriteBatch.Draw(pixelTexture, new Rectangle(alertX, alertY, alertW, alertH), new Color(180, 0, 0, 200));
                PixelFontRenderer.DrawTextCentered(spriteBatch, pixelTexture, "! ALERTA DE ESTOL (STALL) !", screenWidth / 2, alertY + 15, 2, Color.White);
            }
        }

        // 6. Flight Controls Helper Overlay (Top center)
        PixelFontRenderer.DrawTextCentered(spriteBatch, pixelTexture, "V: ALTERNAR CAMERA (C) | REINICIAR (R)", screenWidth / 2, 20, 1, Color.DarkGray);

        // 7. Success / Crash / Ground states
        if (plane.HasCrashed)
        {
            // Dark transparent red screen
            spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, screenWidth, screenHeight), new Color(120, 10, 10, 180));
            PixelFontRenderer.DrawTextCentered(spriteBatch, pixelTexture, "AVIAO DESTRUIDO", screenWidth / 2, screenHeight / 2 - 40, 5, Color.White);
            PixelFontRenderer.DrawTextCentered(spriteBatch, pixelTexture, "VOCE COLIDIU COM O TERRENO OU FEZ UM POUSO FORTE", screenWidth / 2, screenHeight / 2 + 15, 1, Color.LightGray);
            PixelFontRenderer.DrawTextCentered(spriteBatch, pixelTexture, "PRESSIONE R PARA REINICIAR O VOO", screenWidth / 2, screenHeight / 2 + 50, 2, Color.Yellow);
            PixelFontRenderer.DrawTextCentered(spriteBatch, pixelTexture, "PRESSIONE ESC PARA VOLTAR AO MENU", screenWidth / 2, screenHeight / 2 + 85, 2, Color.White);
        }
        else if (isLandedSuccess)
        {
            // Dark transparent green screen
            spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, screenWidth, screenHeight), new Color(10, 100, 20, 200));
            PixelFontRenderer.DrawTextCentered(spriteBatch, pixelTexture, "POUSO BEM SUCEDIDO!", screenWidth / 2, screenHeight / 2 - 45, 5, Color.White);
            PixelFontRenderer.DrawTextCentered(spriteBatch, pixelTexture, "PARABENS! VOCE CONTROLOU A FISICA E POUSOU A SALVO", screenWidth / 2, screenHeight / 2 + 10, 1, Color.LightGray);
            PixelFontRenderer.DrawTextCentered(spriteBatch, pixelTexture, "VELOCIDADE FINAL: 0 KTS", screenWidth / 2, screenHeight / 2 + 35, 2, Color.Yellow);
            PixelFontRenderer.DrawTextCentered(spriteBatch, pixelTexture, "PRESSIONE R PARA DECOLAR NOVAMENTE", screenWidth / 2, screenHeight / 2 + 75, 2, Color.White);
            PixelFontRenderer.DrawTextCentered(spriteBatch, pixelTexture, "PRESSIONE ESC PARA VOLTAR AO MENU", screenWidth / 2, screenHeight / 2 + 110, 2, Color.LightGray);
        }
        else if (plane.IsOnGround && plane.Airspeed < 1.0f && engine.Throttle > 0f)
        {
            // Landing advice before takeoff
            PixelFontRenderer.DrawTextCentered(spriteBatch, pixelTexture, "ACELERE PARA 100% (W) PARA DECOLAR", screenWidth / 2, screenHeight / 2 - 100, 2, Color.Yellow);
            PixelFontRenderer.DrawTextCentered(spriteBatch, pixelTexture, "USE SETA PARA BAIXO PARA LEVANTAR O NARIZ AO PASSAR DE 45 KTS", screenWidth / 2, screenHeight / 2 - 70, 1, Color.LightGray);
        }
    }

    private void DrawGlassPanel(SpriteBatch spriteBatch, Texture2D pixelTexture, int x, int y, int w, int h)
    {
        // Dark translucent slate background
        spriteBatch.Draw(pixelTexture, new Rectangle(x, y, w, h), new Color(15, 23, 42, 190));
        // Subtle outline border
        spriteBatch.Draw(pixelTexture, new Rectangle(x, y, w, 1), new Color(255, 255, 255, 40));
        spriteBatch.Draw(pixelTexture, new Rectangle(x, y + h - 1, w, 1), new Color(255, 255, 255, 40));
        spriteBatch.Draw(pixelTexture, new Rectangle(x, y, 1, h), new Color(255, 255, 255, 40));
        spriteBatch.Draw(pixelTexture, new Rectangle(x + w - 1, y, 1, h), new Color(255, 255, 255, 40));
    }

    private void DrawCrosshair(SpriteBatch spriteBatch, Texture2D pixelTexture, int cx, int cy)
    {
        Color hudColor = new Color(50, 255, 50, 150); // Light green glow

        // Draw central tiny dot
        spriteBatch.Draw(pixelTexture, new Rectangle(cx - 2, cy - 2, 4, 4), hudColor);

        // Draw horizon tick bars on left and right
        int offset = 30;
        int barW = 25;
        spriteBatch.Draw(pixelTexture, new Rectangle(cx - offset - barW, cy, barW, 2), hudColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(cx - offset - barW, cy, 2, 8), hudColor); // left cap

        spriteBatch.Draw(pixelTexture, new Rectangle(cx + offset, cy, barW, 2), hudColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(cx + offset + barW - 2, cy, 2, 8), hudColor); // right cap

        // Draw pitch boresight marker (little wings)
        spriteBatch.Draw(pixelTexture, new Rectangle(cx - 12, cy - 6, 2, 6), hudColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(cx - 12, cy - 6, 8, 2), hudColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(cx + 4, cy - 6, 2, 6), hudColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(cx + 4, cy - 6, 8, 2), hudColor);
    }
}
