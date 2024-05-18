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
        private const string PanelNameContains = "GPS-Map";
        private const int broadcastRadius = 700;
        private TimeSpan refreshInterval = TimeSpan.FromSeconds(2);


        private DateTime lastRefreshTime = DateTime.MinValue;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            
            try
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
            } catch(Exception ex)
            {
                Echo(ex.ToString());
            }
        }

        private void RunScript()
        {
            var lcdPanels = FindLCDPanels();
            var cockpits = FindCockpits();

            if (lcdPanels.Count == 0 && cockpits.Count == 0)
            {
                Echo($"No panel or cockpit with name containing {PanelNameContains} found.");
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
            return lcdPanels.Where(panel => panel.CustomName.Contains(PanelNameContains)).ToList();
        }

        private List<IMyCockpit> FindCockpits()
        {
            List<IMyCockpit> cockpits = new List<IMyCockpit>();
            GridTerminalSystem.GetBlocksOfType(cockpits, cockpit => cockpit.CustomName.Contains(PanelNameContains));
            return cockpits;
        }

        private void DrawMapOnLcd(IMyTextPanel lcdPanel)
        {
            Echo($"Panel: '{lcdPanel.CustomName}'");
            
            PrepareTextSurfaceForSprites(lcdPanel);

            var frame = lcdPanel.DrawFrame();

            DrawingUtils.DrawBackground(ref frame, lcdPanel);
            DrawingUtils.DrawAntenna(ref frame, lcdPanel, DrawingUtils.GetCenter(lcdPanel), 1f, 0f, 1.5f);
            DrawMarkersFromGPS(ref frame, lcdPanel, lcdPanel);

            frame.Dispose();
        }


        private void DrawMapOnCockpit(IMyCockpit cockpit)
        {
            Echo($"Cockpit: '{cockpit.CustomName}'");

            var screenToDraw = cockpit.GetSurface(0);
            PrepareTextSurfaceForSprites(screenToDraw);

            var frame = screenToDraw.DrawFrame();

            DrawingUtils.DrawBackground(ref frame, screenToDraw);
            DrawingUtils.DrawVehicleMark(ref frame, screenToDraw, DrawingUtils.GetCenter(screenToDraw), 1f, 1.5f);
            DrawMarkersFromGPS(ref frame, cockpit, screenToDraw);

            frame.Dispose();
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
                    textSurface.ScriptBackgroundColor = new Color(0,0,0,0);
                }
            }

        }

        private void DrawMarkersFromGPS(ref MySpriteDrawFrame frame, IMyTerminalBlock gpsSource, IMyTextSurface drawingSurface)
        {
            var gpsList = GetGPSList(gpsSource);
            if (gpsList.Count == 0)
            {
                DrawingUtils.DrawError(ref frame, drawingSurface, $"No GPS coordinates in CustomData");
                return;
            }
            Echo($"{gpsList.Count} coordinates");

            var drawingSurfaceRadius = GetLCDPanelRadius(drawingSurface);
            var distanceScale = drawingSurfaceRadius / broadcastRadius;

            foreach (var gps in gpsList)
            {
                var parts = gps.Split(':');
                if (parts.Length >= 5 && parts[0] == "GPS")
                {
                    var name = parts[1];
                    Vector3D gpsPosition = new Vector3D(double.Parse(parts[2]), double.Parse(parts[3]), double.Parse(parts[4]));
                    Vector3D gpsPositionInAntennaLocalCoordinates = Vector3D.Transform(gpsPosition, MatrixD.Invert(gpsSource.WorldMatrix)); ;
                    var proectionTo2D = new Vector2((float)gpsPositionInAntennaLocalCoordinates.X, (float)gpsPositionInAntennaLocalCoordinates.Z);

                    var markerPosition =  proectionTo2D * distanceScale + DrawingUtils.GetCenter(drawingSurface);
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
