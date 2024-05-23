using Sandbox.ModAPI.Ingame;
using System;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class DrawingSurfaceWrapper
        {

            private IMyTextSurface _drawingSurface;
            private MySpriteDrawFrame _frame;
            private Action<string> _echo;

            public DrawingSurfaceWrapper(IMyTextSurface drawingSurface, Action<string> echo)
            {
                this._drawingSurface = drawingSurface;
                _frame = drawingSurface.DrawFrame();
                _echo = echo;

                RandomFlushOfSpriteCache();
                PrepareTextSurfaceForSprites();
                DrawingUtils.DrawBackground(_frame, _drawingSurface);
            }

            private void RandomFlushOfSpriteCache()
            {
                if (DateTime.Now.Millisecond % 2 == 0)
                {
                    _echo("Flushing cache");
                    var clearSprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SquareSimple",
                        Position = _drawingSurface.TextureSize / 2,
                        Size = _drawingSurface.TextureSize,
                        Color = new Color(0, 0, 0, 0) // Fully transparent
                    };
                    _frame.Add(clearSprite);
                }
            }

            private void PrepareTextSurfaceForSprites()
            {
                _drawingSurface.ContentType = ContentType.SCRIPT;
                _drawingSurface.Script = "";

                // enable support for transparent screens, for some reason scripting fills background with non-transparent color
                if (_drawingSurface is IMyCubeBlock)
                {
                    var blockDefinition = ((IMyCubeBlock)_drawingSurface).BlockDefinition.SubtypeId;
                    if (blockDefinition.Contains("Transparent"))
                    {
                        _echo("Background transparency set to 0");
                        _drawingSurface.ScriptBackgroundColor = _drawingSurface.ScriptBackgroundColor.Alpha(1f);
                    }
                }
            }

            public void DrawAntenna()
            {
                DrawingUtils.DrawAntenna(_frame, _drawingSurface, DrawingUtils.GetCenter(_drawingSurface), 1f, 0f, 1.5f);
            }

            public void DrawVehicleMark()
            {
                DrawingUtils.DrawVehicleMark(_frame, _drawingSurface, DrawingUtils.GetCenter(_drawingSurface), 1f, 1.5f);
            }

            public void DrawGpsMarker(Vector2 markerPosition, string name)
            {
                DrawingUtils.DrawMarker(_frame, markerPosition, name);
            }


            public void DrawError(string error)
            {
                DrawingUtils.DrawError(_frame, _drawingSurface, error);
            }

            public float Radius {
                get {
                    var surfaceSize = _drawingSurface.SurfaceSize;
                    return Math.Min(surfaceSize.X, surfaceSize.Y) / 2f;
                }
            }

            public Vector2 Center {
                get {
                    return DrawingUtils.GetCenter(_drawingSurface);
                }
            }

            public void Dispose()
            {
                _frame.Dispose();
                _echo("");
            }
            
        }
    }
}
