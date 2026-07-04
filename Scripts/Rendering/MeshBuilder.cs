using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PlaneSim.Scripts.Rendering;

public static class MeshBuilder
{
    // Helper to calculate flat normal and add a triangle to the vertex list
    public static void AddFlatTriangle(List<VertexPositionNormalColor> vertices, Vector3 p1, Vector3 p2, Vector3 p3, Color color)
    {
        Vector3 edge1 = p2 - p1;
        Vector3 edge2 = p3 - p1;
        Vector3 normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

        // In MonoGame / XNA, standard culling is CounterClockwise.
        // We will add them in order.
        vertices.Add(new VertexPositionNormalColor(p1, normal, color));
        vertices.Add(new VertexPositionNormalColor(p2, normal, color));
        vertices.Add(new VertexPositionNormalColor(p3, normal, color));
    }

    // Helper to add a flat quad (2 triangles)
    public static void AddFlatQuad(List<VertexPositionNormalColor> vertices, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Color color)
    {
        // CCW Triangles: (p1, p2, p3) and (p1, p3, p4)
        AddFlatTriangle(vertices, p1, p2, p3, color);
        AddFlatTriangle(vertices, p1, p3, p4, color);
    }

    // Generates a mountainous terrain grid with a flat area in the center (where the runway resides)
    public static VertexPositionNormalColor[] CreateTerrain(int segmentsX, int segmentsZ, float sizeX, float sizeZ)
    {
        List<VertexPositionNormalColor> vertices = new List<VertexPositionNormalColor>();

        float halfX = sizeX / 2f;
        float halfZ = sizeZ / 2f;
        float dx = sizeX / segmentsX;
        float dz = sizeZ / segmentsZ;

        // Get height at a specific (x, z) location
        float GetHeight(float x, float z)
        {
            float zLimit = 850f; // Flat valley corridor length
            float dz_dist = 0f;
            if (z > zLimit) dz_dist = z - zLimit;
            else if (z < -zLimit) dz_dist = -zLimit - z;

            float distanceToRunway = (float)Math.Sqrt(x * x + dz_dist * dz_dist);
            // Flat valley corridor: damp height to 0 within 450m of the runway line
            float damp = 1.0f - (float)Math.Exp(-(distanceToRunway * distanceToRunway) / (450f * 450f));

            // Stacked noise waves for natural-looking low-poly mountains
            double height = (Math.Sin(x * 0.0015f) * Math.Cos(z * 0.0015f) * 350.0) +
                            (Math.Sin(x * 0.004f + z * 0.003f) * 120.0) +
                            (Math.Cos(x * 0.0004f) * Math.Sin(z * 0.0004f) * 700.0);

            float h = (float)(height * damp);
            return Math.Max(h, -5f); // Floor level
        }

        // Get color based on height and slope
        Color GetColorForHeight(float h)
        {
            if (h > 450f)
            {
                // Snowy peaks
                return Color.Lerp(new Color(220, 220, 230), Color.White, (h - 450f) / 150f);
            }
            else if (h > 150f)
            {
                // Stone/mountain gray-brown
                return Color.Lerp(new Color(90, 85, 80), new Color(160, 160, 160), (h - 150f) / 300f);
            }
            else if (h > 20f)
            {
                // Normal green hills
                return Color.Lerp(new Color(34, 139, 34), new Color(46, 110, 46), h / 150f);
            }
            else
            {
                // Lush valley grass
                return new Color(34, 139, 34); // Forest Green
            }
        }

        for (int z = 0; z < segmentsZ; z++)
        {
            for (int x = 0; x < segmentsX; x++)
            {
                // 4 corners of the grid cell
                float xL = -halfX + x * dx;
                float xR = -halfX + (x + 1) * dx;
                float zB = -halfZ + z * dz;
                float zF = -halfZ + (z + 1) * dz;

                float yLB = GetHeight(xL, zB);
                float yRB = GetHeight(xR, zB);
                float yLF = GetHeight(xL, zF);
                float yRF = GetHeight(xR, zF);

                Vector3 pLB = new Vector3(xL, yLB, zB);
                Vector3 pRB = new Vector3(xR, yRB, zB);
                Vector3 pLF = new Vector3(xL, yLF, zF);
                Vector3 pRF = new Vector3(xR, yRF, zF);

                // Determine color of cell (average height of corners)
                float avgHeight = (yLB + yRB + yLF + yRF) / 4f;
                Color cellColor = GetColorForHeight(avgHeight);

                // Add cell triangles: LB -> LF -> RB and RB -> LF -> RF
                AddFlatTriangle(vertices, pLB, pLF, pRB, cellColor);
                AddFlatTriangle(vertices, pRB, pLF, pRF, cellColor);
            }
        }

        return vertices.ToArray();
    }

    // Generates the landing strip per GDDD: Positioned at Y=0, aligned along North (Z-axis)
    public static VertexPositionNormalColor[] CreateRunway()
    {
        List<VertexPositionNormalColor> vertices = new List<VertexPositionNormalColor>();

        float w = 40f; // runway width
        float len = 1200f; // runway length
        float halfLen = len / 2f;
        float y = 0.05f; // slightly raised above terrain to avoid z-fighting

        Color runwayColor = new Color(50, 50, 50); // Dark gray asphalt
        Color lineWhite = new Color(240, 240, 240); // White lines

        // 1. Runway Main Asphalt Deck
        Vector3 deckLB = new Vector3(-w / 2f, y, -halfLen);
        Vector3 deckRB = new Vector3(w / 2f, y, -halfLen);
        Vector3 deckLF = new Vector3(-w / 2f, y, halfLen);
        Vector3 deckRF = new Vector3(w / 2f, y, halfLen);
        AddFlatQuad(vertices, deckLB, deckLF, deckRF, deckRB, runwayColor);

        // 2. White Borders (Left & Right margins)
        float borderW = 1.0f;
        // Left Border
        AddFlatQuad(vertices,
            new Vector3(-w / 2f + 0.5f, y + 0.01f, -halfLen + 20f),
            new Vector3(-w / 2f + 0.5f, y + 0.01f, halfLen - 20f),
            new Vector3(-w / 2f + 0.5f + borderW, y + 0.01f, halfLen - 20f),
            new Vector3(-w / 2f + 0.5f + borderW, y + 0.01f, -halfLen + 20f),
            lineWhite);

        // Right Border
        AddFlatQuad(vertices,
            new Vector3(w / 2f - 0.5f - borderW, y + 0.01f, -halfLen + 20f),
            new Vector3(w / 2f - 0.5f - borderW, y + 0.01f, halfLen - 20f),
            new Vector3(w / 2f - 0.5f, y + 0.01f, halfLen - 20f),
            new Vector3(w / 2f - 0.5f, y + 0.01f, -halfLen + 20f),
            lineWhite);

        // 3. Centerline Dashed Stripes
        float dashLen = 25f;
        float gapLen = 25f;
        float dashW = 0.6f;
        for (float z = -halfLen + 50f; z < halfLen - 50f; z += (dashLen + gapLen))
        {
            AddFlatQuad(vertices,
                new Vector3(-dashW / 2f, y + 0.01f, z),
                new Vector3(-dashW / 2f, y + 0.01f, z + dashLen),
                new Vector3(dashW / 2f, y + 0.01f, z + dashLen),
                new Vector3(dashW / 2f, y + 0.01f, z),
                lineWhite);
        }

        // 4. Threshold Stripes (Piano keys at runway ends)
        void AddThresholdStripes(float zCenter, float sign)
        {
            float stripeW = 1.5f;
            float stripeH = 30f;
            float spacing = 2.5f;
            int numStripes = 8;
            float startX = -((numStripes - 1) * spacing + stripeW) / 2f;

            for (int i = 0; i < numStripes; i++)
            {
                float sx = startX + i * spacing;
                float zStart = zCenter - (stripeH / 2f) * sign;
                float zEnd = zCenter + (stripeH / 2f) * sign;

                AddFlatQuad(vertices,
                    new Vector3(sx, y + 0.01f, zStart),
                    new Vector3(sx, y + 0.01f, zEnd),
                    new Vector3(sx + stripeW, y + 0.01f, zEnd),
                    new Vector3(sx + stripeW, y + 0.01f, zStart),
                    lineWhite);
            }
        }

        AddThresholdStripes(-halfLen + 50f, 1f);
        AddThresholdStripes(halfLen - 50f, -1f);

        return vertices.ToArray();
    }

    // Creates the aircraft model as a single flat-shaded low-poly vertex list
    // Note: MonoGame coordinates are +Y Up, +X Right, +Z Back (so Forward is -Z, like OpenGL/XNA standards)
    // We will align the nose to -Z (Forward)
    public static VertexPositionNormalColor[] CreateAirplane(float propAngleRad)
    {
        List<VertexPositionNormalColor> vertices = new List<VertexPositionNormalColor>();

        Color bodyColor = Color.White;
        Color wingColor = new Color(240, 240, 240); // Gainsboro / soft light gray
        Color cockpitColor = new Color(50, 120, 220, 180); // Glassy blue
        Color darkColor = new Color(30, 30, 30); // Tires, engine cover, details
        Color propellerColor = Color.Black;

        // --- FUSELAGE (Box-like shape tapering to nose and tail) ---
        // Coordinates in local space: X=right, Y=up, Z=forward (nose is negative Z: -Z is forward, Z=nose is at -3.0f, tail is at +3.0f)
        
        Vector3 fNose = new Vector3(0f, 0f, -3.2f); // Nose tip
        
        // Mid sections (fuselage box)
        Vector3 mTopL = new Vector3(-0.35f, 0.4f, -1.0f);
        Vector3 mTopR = new Vector3(0.35f, 0.4f, -1.0f);
        Vector3 mBotL = new Vector3(-0.35f, -0.35f, -1.0f);
        Vector3 mBotR = new Vector3(0.35f, -0.35f, -1.0f);

        Vector3 bTopL = new Vector3(-0.25f, 0.3f, 1.2f);
        Vector3 bTopR = new Vector3(0.25f, 0.3f, 1.2f);
        Vector3 bBotL = new Vector3(-0.25f, -0.25f, 1.2f);
        Vector3 bBotR = new Vector3(0.25f, -0.25f, 1.2f);

        Vector3 fTail = new Vector3(0f, 0.15f, 3.2f); // Tail end

        // 1. Nose Cone (Front cabin section to Nose Tip)
        AddFlatTriangle(vertices, fNose, mTopR, mTopL, bodyColor); // Nose top
        AddFlatTriangle(vertices, fNose, mBotL, mBotR, bodyColor); // Nose bottom
        AddFlatTriangle(vertices, fNose, mTopL, mBotL, bodyColor); // Nose left
        AddFlatTriangle(vertices, fNose, mBotR, mTopR, bodyColor); // Nose right

        // 2. Fuselage Main Cabin Box (Mid section to Back section)
        AddFlatQuad(vertices, mTopL, mTopR, bTopR, bTopL, bodyColor); // Top
        AddFlatQuad(vertices, mBotR, mBotL, bBotL, bBotR, bodyColor); // Bottom
        AddFlatQuad(vertices, mTopL, bTopL, bBotL, mBotL, bodyColor); // Left
        AddFlatQuad(vertices, mTopR, mBotR, bBotR, bTopR, bodyColor); // Right

        // 3. Fuselage Tail Section (Back section to Tail End)
        AddFlatTriangle(vertices, fTail, bTopL, bTopR, bodyColor); // Tail top
        AddFlatTriangle(vertices, fTail, bBotR, bBotL, bodyColor); // Tail bottom
        AddFlatTriangle(vertices, fTail, bTopR, bBotR, bodyColor); // Tail right
        AddFlatTriangle(vertices, fTail, bBotL, bTopL, bodyColor); // Tail left

        // --- MAIN WING ---
        // Position: attached around mid section (Z ~ -0.4f), spans wide X direction.
        // Dihedral angle: wingtip is slightly higher than root.
        float wingSpan = 7.0f;
        float rootChord = 1.0f;
        float tipChord = 0.6f;
        float rootZ = -0.4f;
        float dihedral = 0.3f; // Y offset at wingtips

        // Left Wing (X goes from 0 to -wingSpan)
        Vector3 wlRootF = new Vector3(-0.35f, 0.0f, rootZ - rootChord / 2f);
        Vector3 wlRootB = new Vector3(-0.35f, 0.0f, rootZ + rootChord / 2f);
        Vector3 wlTipF = new Vector3(-wingSpan, dihedral, rootZ - tipChord / 2f);
        Vector3 wlTipB = new Vector3(-wingSpan, dihedral, rootZ + tipChord / 2f);

        // Quad for wing body
        AddFlatQuad(vertices, wlRootB, wlTipB, wlTipF, wlRootF, wingColor);

        // Right Wing (X goes from 0 to +wingSpan)
        Vector3 wrRootF = new Vector3(0.35f, 0.0f, rootZ - rootChord / 2f);
        Vector3 wrRootB = new Vector3(0.35f, 0.0f, rootZ + rootChord / 2f);
        Vector3 wrTipF = new Vector3(wingSpan, dihedral, rootZ - tipChord / 2f);
        Vector3 wrTipB = new Vector3(wingSpan, dihedral, rootZ + tipChord / 2f);

        AddFlatQuad(vertices, wrRootF, wrTipF, wrTipB, wrRootB, wingColor);

        // --- TAIL STABILIZERS ---
        // Horizontal Stabilizer (Elevators) at back: Z ~ 2.6f
        float tailSpan = 1.8f;
        float tailChord = 0.5f;
        float tailZ = 2.6f;

        Vector3 tRootLF = new Vector3(-0.1f, 0.15f, tailZ - tailChord / 2f);
        Vector3 tRootLB = new Vector3(-0.1f, 0.15f, tailZ + tailChord / 2f);
        Vector3 tTipL = new Vector3(-tailSpan, 0.15f, tailZ);
        AddFlatTriangle(vertices, tRootLF, tTipL, tRootLB, wingColor);

        Vector3 tRootRF = new Vector3(0.1f, 0.15f, tailZ - tailChord / 2f);
        Vector3 tRootRB = new Vector3(0.1f, 0.15f, tailZ + tailChord / 2f);
        Vector3 tTipR = new Vector3(tailSpan, 0.15f, tailZ);
        AddFlatTriangle(vertices, tRootRF, tRootRB, tTipR, wingColor);

        // Vertical Fin (Rudder) on top at back: Z ~ 2.6f
        Vector3 finRootF = new Vector3(0f, 0.25f, tailZ - 0.4f);
        Vector3 finRootB = new Vector3(0f, 0.25f, tailZ + 0.2f);
        Vector3 finTip = new Vector3(0f, 1.2f, tailZ + 0.4f);
        AddFlatTriangle(vertices, finRootF, finTip, finRootB, wingColor);

        // --- COCKPIT CANOPY ---
        // Raised cockpit glass on top mid section
        Vector3 cFront = new Vector3(0f, 0.7f, -1.0f);
        Vector3 cBack = new Vector3(0f, 0.6f, 0.2f);
        Vector3 cLeft = new Vector3(-0.25f, 0.4f, -0.4f);
        Vector3 cRight = new Vector3(0.25f, 0.4f, -0.4f);

        AddFlatTriangle(vertices, cFront, cRight, cLeft, cockpitColor); // windshield
        AddFlatTriangle(vertices, cBack, cLeft, cRight, cockpitColor); // back glass
        AddFlatTriangle(vertices, cFront, cLeft, cBack, cockpitColor); // side left
        AddFlatTriangle(vertices, cFront, cBack, cRight, cockpitColor); // side right

        // --- PROPELLER ---
        // Propeller spinner center (nose nose cone tip)
        Vector3 pCenter = new Vector3(0f, 0f, -3.22f);
        float propRadius = 1.3f;
        float propWidth = 0.08f;

        // Calculate blade ends based on rotation angle
        float cos = (float)Math.Cos(propAngleRad);
        float sin = (float)Math.Sin(propAngleRad);

        // Blade 1 Vector (rotated X/Y plane)
        Vector3 propVector1 = new Vector3(cos * propRadius, sin * propRadius, 0f);
        Vector3 propVector2 = new Vector3(-sin * propWidth, cos * propWidth, 0f);

        Vector3 p1a = pCenter + propVector1 - propVector2;
        Vector3 p1b = pCenter + propVector1 + propVector2;
        Vector3 p1c = pCenter - propVector2;
        Vector3 p1d = pCenter + propVector2;

        AddFlatQuad(vertices, p1c, p1a, p1b, p1d, propellerColor);

        // Blade 2 (Opposite)
        Vector3 p2a = pCenter - propVector1 - propVector2;
        Vector3 p2b = pCenter - propVector1 + propVector2;

        AddFlatQuad(vertices, p1d, p2b, p2a, p1c, propellerColor);

        // Prop Spinner nose cover (small black box)
        Vector3 sNose = new Vector3(0f, 0f, -3.32f);
        Vector3 sL = new Vector3(-0.1f, 0.1f, -3.2f);
        Vector3 sR = new Vector3(0.1f, 0.1f, -3.2f);
        Vector3 sB = new Vector3(0f, -0.1f, -3.2f);
        AddFlatTriangle(vertices, sNose, sR, sL, darkColor);
        AddFlatTriangle(vertices, sNose, sB, sR, darkColor);
        AddFlatTriangle(vertices, sNose, sL, sB, darkColor);

        return vertices.ToArray();
    }
}
