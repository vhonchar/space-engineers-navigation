using Sandbox.Game.EntityComponents;
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
    partial class Program : MyGridProgram
    {
        private const string PanelNameContains = "[GPS]";
        private const int broadcastRadius = 700;
        private TimeSpan refreshInterval = TimeSpan.FromSeconds(2);


        private DateTime lastRefreshTime = DateTime.MinValue;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument)
        {

            if ((DateTime.Now - lastRefreshTime) < refreshInterval)
            {
                lastRefreshTime = DateTime.Now;
                return;
            }

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
            }

            foreach (var cockpit in cockpits)
            {
                DrawMapOnCockpit(cockpit);
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
            Echo($"Drawing on panel '{lcdPanel.CustomName}'");
            var drawingSurface = lcdPanel;
            PrepareTextSurfaceForSprites(drawingSurface);

            var frame = drawingSurface.DrawFrame();

            DrawingUtils.DrawBackground(ref frame, lcdPanel);
            DrawingUtils.DrawAntenna(ref frame, lcdPanel, DrawingUtils.GetCenter(lcdPanel), 1f, 0f, 1.5f);
            DrawMarkersFromGPS(ref frame, lcdPanel, lcdPanel);

            frame.Dispose();
        }


        private void DrawMapOnCockpit(IMyCockpit cockpit)
        {
            Echo($"Drawing on cockpit '{cockpit.CustomName}'");

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
            // Set the sprite display mode
            textSurface.ContentType = ContentType.SCRIPT;
            // Make sure no built-in script has been selected
            textSurface.Script = "";

            // enable support for transparent screens, for some reason scripting fills background with non-transparent color
            if(textSurface is IMyCubeBlock)
            {
                var blockDefinition = ((IMyCubeBlock)textSurface).BlockDefinition.SubtypeId;
                Echo($"Defition ID of the surface {blockDefinition}");
                if (blockDefinition.Contains("Transparent"))
                {
                    textSurface.BackgroundAlpha = 0;
                }
            }

        }

        private void DrawMarkersFromGPS(ref MySpriteDrawFrame frame, IMyTerminalBlock gpsSource, IMyTextSurface drawingSurface)
        {
            var gpsList = GetGPSList(gpsSource);
            if (gpsList.Count == 0)
            {
                DrawingUtils.DrawError(ref frame, drawingSurface, $"No GPS coordinates in CustomData\nof panel '{gpsSource.CustomName}'");
                return;
            }

            var drawingSurfaceRadius = GetLCDPanelRadius(drawingSurface);
            Echo($"Drawing Surface Radius: {drawingSurfaceRadius}");
            var distanceScale = drawingSurfaceRadius / broadcastRadius;
            Echo($"Distance Scale: {distanceScale}");

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
                    DrawingUtils.DrawMarker(ref frame, markerPosition, name, 1f, 0f, 1f);
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
