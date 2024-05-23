using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private const int DefaultDetectionRadius = 700;
        private TimeSpan refreshInterval = TimeSpan.FromSeconds(2);

        private DateTime lastRefreshTime = DateTime.MinValue;
        private ProgrammConfig programmConfig;

        public Program()
        {
            programmConfig = new ProgrammConfig(Me);
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if ((DateTime.Now - lastRefreshTime) < refreshInterval)
            {
                return;
            }
            Echo($"Running at {lastRefreshTime.TimeOfDay}");
            var start = DateTime.Now;

            RunScript();
            lastRefreshTime = DateTime.Now;
            Echo($"Complete run in {(lastRefreshTime - start).TotalMilliseconds}ms");
        }
        

        private void RunScript()
        {
            var lcdPanels = FindBlocksContainingInName<IMyTextPanel>(programmConfig.PanelNameIdentifier);
            var cockpits = FindBlocksContainingInName<IMyCockpit>(programmConfig.PanelNameIdentifier);

            if (lcdPanels.Count == 0 && cockpits.Count == 0)
            {
                Echo($"No panel or cockpit with name containing {programmConfig.PanelNameIdentifier} found.");
                return;
            }

            foreach (var lcdPanel in lcdPanels)
            {
                DrawMapOnLcd(lcdPanel);
                Echo("");
            }

            foreach (var cockpit in cockpits)
            {
                DrawMapOnCockpit(cockpit);
                Echo("");
            }
        }

        private List<T> FindBlocksContainingInName<T>(string nameContains) where T : class, IMyTerminalBlock
        {
            List<T> cockpits = new List<T>();
            GridTerminalSystem.GetBlocksOfType(cockpits, cockpit => cockpit.CustomName.Contains(nameContains));
            return cockpits;
        }

        private void DrawMapOnLcd(IMyTextPanel lcdPanel)
        {
            Echo($"Panel: '{lcdPanel.CustomName}'");

            int detectionRaius = GetDetectionRadius(lcdPanel);

            PrepareTextSurfaceForSprites(lcdPanel);

            var frame = lcdPanel.DrawFrame();

            RandomFlushOfSpriteCache(frame, lcdPanel);
            DrawingUtils.DrawBackground(ref frame, lcdPanel);
            DrawingUtils.DrawAntenna(ref frame, lcdPanel, DrawingUtils.GetCenter(lcdPanel), 1f, 0f, 1.5f);
            DrawMarkersFromGPS(ref frame, lcdPanel, lcdPanel, detectionRaius);

            frame.Dispose();
        }

        private void DrawMapOnCockpit(IMyCockpit cockpit)
        {
            Echo($"Cockpit: '{cockpit.CustomName}'");

            int broadcastRadius = GetDetectionRadius(cockpit);

            var screenToDraw = cockpit.GetSurface(0);
            PrepareTextSurfaceForSprites(screenToDraw);

            var frame = screenToDraw.DrawFrame();

            RandomFlushOfSpriteCache(frame, screenToDraw);
            DrawingUtils.DrawBackground(ref frame, screenToDraw);
            DrawingUtils.DrawVehicleMark(ref frame, screenToDraw, DrawingUtils.GetCenter(screenToDraw), 1f, 1.5f);
            DrawMarkersFromGPS(ref frame, cockpit, screenToDraw, broadcastRadius);

            frame.Dispose();
        }

        private int GetDetectionRadius(IMyTerminalBlock block)
        {
            var configuredDetectionRadius = block.CustomData
                .Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(line => line.StartsWith("DetectionDistance"))
                ?.Split('=')[1];

            if(string.IsNullOrEmpty(configuredDetectionRadius))
            {
                block.CustomData = $"DetectionDistance={DefaultDetectionRadius}\n" + block.CustomData;
                return DefaultDetectionRadius;
            }
            return int.Parse(configuredDetectionRadius);
        }

        private void RandomFlushOfSpriteCache(MySpriteDrawFrame frame, IMyTextSurface drawingSurface)
        {
            if (DateTime.Now.Millisecond % 2 == 0)
            {
                Echo("Flushing cache");
                var clearSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = drawingSurface.TextureSize / 2,
                    Size = drawingSurface.TextureSize,
                    Color = new Color(0, 0, 0, 0) // Fully transparent
                };
                frame.Add(clearSprite);
            }
        }

        private void PrepareTextSurfaceForSprites(IMyTextSurface textSurface)
        {
            textSurface.ContentType = ContentType.SCRIPT;
            textSurface.Script = "";

            // enable support for transparent screens, for some reason scripting fills background with non-transparent color
            if(textSurface is IMyCubeBlock)
            {
                var blockDefinition = ((IMyCubeBlock)textSurface).BlockDefinition.SubtypeId;
                if (blockDefinition.Contains("Transparent"))
                {
                    Echo("Background transparency set to 0");
                    textSurface.ScriptBackgroundColor = textSurface.ScriptBackgroundColor.Alpha(1f);
                }
            }
        }

        private void DrawMarkersFromGPS(ref MySpriteDrawFrame frame, IMyTerminalBlock gpsSource, IMyTextSurface drawingSurface, int detectionRadius)
        {
            var gpsList = GetGPSList(gpsSource);
            if (gpsList.Count == 0)
            {
                DrawingUtils.DrawError(ref frame, drawingSurface, $"No GPS coordinates in CustomData");
                return;
            }
            Echo($"{gpsList.Count} coordinates");

            var drawingSurfaceRadius = GetLCDPanelRadius(drawingSurface);
            var distanceScale = drawingSurfaceRadius / detectionRadius;

            foreach (var gpsMark in gpsList)
            {
                Vector3D gpsPositionInAntennaLocalCoordinates = Vector3D.Transform(gpsMark.Coords, MatrixD.Invert(gpsSource.WorldMatrix));
                var projectionTo2D = new Vector2((float)gpsPositionInAntennaLocalCoordinates.X, (float)gpsPositionInAntennaLocalCoordinates.Z);

                var markerPosition = projectionTo2D * distanceScale + DrawingUtils.GetCenter(drawingSurface);
                DrawingUtils.DrawMarker(ref frame, markerPosition, gpsMark.Name);
            }
        }

        private List<GpsMark> GetGPSList(IMyTerminalBlock lcdPanel)
        {
            var customData = lcdPanel.CustomData;
            return lcdPanel.CustomData
                .Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .ToList()
                .FindAll(it => GpsMark.IsGpsMark(it))
                .ConvertAll(it => GpsMark.FromString(it));
        }

        private float GetLCDPanelRadius(IMyTextSurface lcdPanel)
        {
            var surfaceSize = lcdPanel.SurfaceSize;
            return Math.Min(surfaceSize.X, surfaceSize.Y) / 2f;
        }
    }
}
