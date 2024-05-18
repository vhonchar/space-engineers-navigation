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

            RandomFlushOfSpriteCache(frame, lcdPanel);
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

            RandomFlushOfSpriteCache(frame, screenToDraw);
            DrawingUtils.DrawBackground(ref frame, screenToDraw);
            DrawingUtils.DrawVehicleMark(ref frame, screenToDraw, DrawingUtils.GetCenter(screenToDraw), 1f, 1.5f);
            DrawMarkersFromGPS(ref frame, cockpit, screenToDraw);

            frame.Dispose();
        }

        // Game caches list of sprites which are sent to the game client
        // and doesn't resent a sprite, if its state OR position in anarray of all sprites doesn't change.
        // And like all systems, game has issues with flushing the cache in multiple cases
        //
        // This function will randomly add a blank sprite into that array of sprites, forcing server to resend all sprites after that.
        // Thus, the script will refresh entire screen once in a while
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
