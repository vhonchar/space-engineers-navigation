﻿using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class DrawingUtils
        {
            private static float DEFAULT_TRANSPARANCY = 66;
            public static void DrawMarker(MySpriteDrawFrame frame, Vector2 centerPos, string name, float scale = 1f, float rotation = 0f, float colorScale = 1f)
            {
                float sin = (float)Math.Sin(rotation);
                float cos = (float)Math.Cos(rotation);
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-sin * -5f, cos * -5f) * scale + centerPos, new Vector2(2f, 10f) * scale, new Color(0.5019608f * colorScale, 1f * colorScale, 0.5019608f * colorScale, 1f), null, TextAlignment.CENTER, rotation)); // pointer
                frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(cos * -1f - sin * -14f, sin * -1f + cos * -14f) * scale + centerPos, new Vector2(10f, 10f) * scale, new Color(0.5019608f * colorScale, 1f * colorScale, 0.5019608f * colorScale, 1f), null, TextAlignment.CENTER, 3.1416f + rotation)); // arrow
                frame.Add(new MySprite(SpriteType.TEXT, name, new Vector2(-sin * -36f, cos * -36f) * scale + centerPos, null, new Color(0.5019608f * colorScale, 1f * colorScale, 0.5019608f * colorScale, 1f), "Debug", TextAlignment.LEFT, 0.5f * scale)); // name
            }

            public static void DrawError(MySpriteDrawFrame frame, IMyTextSurface textSerface, string message)
            {
                var position = new Vector2(textSerface.SurfaceSize.X / 2, textSerface.SurfaceSize.Y / 4);
                frame.Add(new MySprite(SpriteType.TEXT, message, position, null, null, "Red", TextAlignment.CENTER, 1f));
            }

            public static void DrawBackground(MySpriteDrawFrame frame, IMyTextSurface drawingSurface)
            {
                var size = drawingSurface.SurfaceSize;
                frame.Add(new MySprite(SpriteType.TEXTURE, "Grid", GetCenter(drawingSurface), size, drawingSurface.ScriptForegroundColor.Alpha(DEFAULT_TRANSPARANCY), null, TextAlignment.CENTER, 0f));
            }

            public static void DrawAntenna(MySpriteDrawFrame frame, IMyTextSurface drawingSerface, Vector2 centerPos, float scale = 1f, float rotation = 0f, float colorScale = 1f)
            {
                float sin = (float)Math.Sin(rotation);
                float cos = (float)Math.Cos(rotation);
                Color color = drawingSerface.ScriptForegroundColor.Alpha(DEFAULT_TRANSPARANCY) * colorScale;
                frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(cos * -1f - sin * 8f, sin * -1f + cos * 8f) * scale + centerPos, new Vector2(10f, 30f) * scale, color, null, TextAlignment.CENTER, rotation)); // base
                frame.Add(new MySprite(SpriteType.TEXTURE, "SemiCircle", new Vector2(cos * 4f - sin * -7f, sin * 4f + cos * -7f) * scale + centerPos, new Vector2(35f, 20f) * scale, color, null, TextAlignment.CENTER, 4.0143f + rotation)); // dish
                frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(cos * 5f - sin * -7f, sin * 5f + cos * -7f) * scale + centerPos, new Vector2(5f, 20f) * scale, color, null, TextAlignment.CENTER, 0.7854f + rotation)); // antenna
            }

            public static void DrawVehicleMark(MySpriteDrawFrame frame, IMyTextSurface lcdPanel, Vector2 centerPos, float scale = 1f, float colorScale = 1f)
            {
                Color color = lcdPanel.ScriptForegroundColor.Alpha(DEFAULT_TRANSPARANCY) * colorScale;
                frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(0f, 0f) * scale + centerPos, new Vector2(10f, 20f) * scale, color, null, TextAlignment.CENTER, 0f));
            }

            public static Vector2 GetCenter(IMyTextSurface surface)
            {
                return surface.TextureSize / 2;
            }
        }
    }
}
