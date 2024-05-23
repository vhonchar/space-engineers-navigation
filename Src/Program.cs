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
        private string panelNameContains;
        private const int DefaultDetectionRadius = 700;
        private TimeSpan refreshInterval = TimeSpan.FromSeconds(2);

        private DateTime lastRefreshTime = DateTime.MinValue;

        public Program()
        {
            InitializeCustomData();
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

        private void InitializeCustomData()
        {
            if (!Me.CustomData.Contains("PanelNameContains"))
            {
                Me.CustomData = "PanelNameContains=GPS-Map\n" + Me.CustomData;
            }

            panelNameContains = Me.CustomData
                .Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(line => line.StartsWith("PanelNameContains"))
                ?.Split('=')[1]
                ?? "GPS-Map";
        }

        private void RunScript()
        {
            var lcdPanels = FindLCDPanels();
            var cockpits = FindCockpits();

            if (lcdPanels.Count == 0 && cockpits.Count == 0)
            {
                Echo($"No panel or cockpit with name containing {panelNameContains} found.");
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

        private List<IMyTextPanel> FindLCDPanels()
        {
            List<IMyTextPanel> lcdPanels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(lcdPanels);
            return lcdPanels.Where(panel => panel.CustomName.Contains(panelNameContains)).ToList();
        }

        private List<IMyCockpit> FindCockpits()
        {
            List<IMyCockpit> cockpits = new List<IMyCockpit>();
            GridTerminalSystem.GetBlocksOfType(cockpits, cockpit => cockpit.CustomName.Contains(panelNameContains));
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

            foreach (var gps in gpsList)
            {
                var parts = gps.Split(':');
                if (parts.Length >= 5 && parts[0] == "GPS")
                {
                    var name = parts[1];
                    Vector3D gpsPosition = new Vector3D(double.Parse(parts[2]), double.Parse(parts[3]), double.Parse(parts[4]));
                    Vector3D gpsPositionInAntennaLocalCoordinates = Vector3D.Transform(gpsPosition, MatrixD.Invert(gpsSource.WorldMatrix));
                    var projectionTo2D = new Vector2((float)gpsPositionInAntennaLocalCoordinates.X, (float)gpsPositionInAntennaLocalCoordinates.Z);

                    var markerPosition = projectionTo2D * distanceScale + DrawingUtils.GetCenter(drawingSurface);
                    DrawingUtils.DrawMarker(ref frame, markerPosition, name);
                }
            }
        }

        private List<string> GetGPSList(IMyTerminalBlock lcdPanel)
        {
            var customData = lcdPanel.CustomData;
            var gpsList = customData.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            return gpsList;
        }

        private float GetLCDPanelRadius(IMyTextSurface lcdPanel)
        {
            var surfaceSize = lcdPanel.SurfaceSize;
            return Math.Min(surfaceSize.X, surfaceSize.Y) / 2f;
        }
    }
}
