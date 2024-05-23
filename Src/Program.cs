using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
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

            DrawMapsWithGpsCoords();
            
            lastRefreshTime = DateTime.Now;
            Echo($"Complete run in {(lastRefreshTime - start).TotalMilliseconds}ms");
        }
        

        private void DrawMapsWithGpsCoords()
        {
            var lcdPanels = FindBlocksContainingInName<IMyTextPanel>(programmConfig.PanelNameIdentifier);
            var cockpits = FindBlocksContainingInName<IMyCockpit>(programmConfig.PanelNameIdentifier);

            if (lcdPanels.Count == 0 && cockpits.Count == 0)
            {
                Echo($"No panel or cockpit with name containing {programmConfig.PanelNameIdentifier} found.");
                return;
            }

            lcdPanels.ForEach(this.DrawMapOnLcd);
            cockpits.ForEach(this.DrawMapOnCockpit);
        }

        private List<T> FindBlocksContainingInName<T>(string nameContains) where T : class, IMyTerminalBlock
        {
            List<T> result = new List<T>();
            GridTerminalSystem.GetBlocksOfType(result, block => block.CustomName.Contains(nameContains) && (!programmConfig.SearchLocalGridOnly || block.CubeGrid == Me.CubeGrid));
            return result;
        }

        private void DrawMapOnLcd(IMyTextPanel lcdPanel)
        {
            Echo($"Panel: '{lcdPanel.CustomName}'");

            var drawingSurface = new DrawingSurfaceWrapper(lcdPanel, Echo);
            var config = new MapConfig(lcdPanel);

            drawingSurface.DrawAntenna();
            DrawMarkersFromGPS(drawingSurface, config, lcdPanel.WorldMatrix);

            drawingSurface.Dispose();
        }

        private void DrawMapOnCockpit(IMyCockpit cockpit)
        {
            Echo($"Cockpit: '{cockpit.CustomName}'");

            var config = new MapConfig(cockpit);
            var drawingSurface = new DrawingSurfaceWrapper(cockpit.GetSurface(config.ScreenNumber), Echo);

            drawingSurface.DrawVehicleMark();
            DrawMarkersFromGPS(drawingSurface, config, cockpit.WorldMatrix);

            drawingSurface.Dispose();
        }

        private void DrawMarkersFromGPS(DrawingSurfaceWrapper drawingSurface, MapConfig config, MatrixD worldMatrixOfBlock)
        {
            if (config.GpsMarks.Count == 0)
            {
                drawingSurface.DrawError($"No GPS coordinates in CustomData");
                return;
            }
            Echo($"{config.GpsMarks.Count} coordinates");

            var drawingSurfaceRadius = drawingSurface.Radius;
            var distanceScale = drawingSurfaceRadius / config.DetectionDistance;

            foreach (var gpsMark in config.GpsMarks)
            {
                Vector3D gpsPositionInAntennaLocalCoordinates = Vector3D.Transform(gpsMark.Coords, MatrixD.Invert(worldMatrixOfBlock));
                var projectionTo2D = new Vector2((float)gpsPositionInAntennaLocalCoordinates.X, (float)gpsPositionInAntennaLocalCoordinates.Z);

                var markerPosition = projectionTo2D * distanceScale + drawingSurface.Center;
                drawingSurface.DrawGpsMarker(markerPosition, gpsMark.Name);
            }
        }
    }
}
