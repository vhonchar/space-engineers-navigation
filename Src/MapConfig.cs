
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    partial class Program
    {
        public class MapConfig
        {
            private static string DetectionDistanceAttrName = "DetectionDistance";
            private static string DetectionDistanceDefault = "700";
            
            public int DetectionDistance { get; }
            public bool SearchLocalGridOnly { get; }
            public List<GpsMark> GpsMarks { get; }

            public MapConfig(IMyTerminalBlock mapTerminalBlock)
            {
                InitializeCustomData(mapTerminalBlock);

                DetectionDistance = int.Parse(GetConfigField(mapTerminalBlock, DetectionDistanceAttrName) ?? DetectionDistanceDefault);
                GpsMarks = GetGPSList(mapTerminalBlock);
            }

            private void InitializeCustomData(IMyTerminalBlock mapTerminalBlock)
            {
                if (!mapTerminalBlock.CustomData.Contains(DetectionDistanceAttrName))
                {
                    mapTerminalBlock.CustomData = $"{DetectionDistanceAttrName}={DetectionDistanceDefault}\n\n" + mapTerminalBlock.CustomData;
                }
            }

            private string GetConfigField(IMyTerminalBlock mapTerminalBlock, string AttrName)
            {
                return mapTerminalBlock.CustomData
                    .Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .First(line => line.StartsWith(AttrName))
                    ?.Split('=')[1];
            }

            private List<GpsMark> GetGPSList(IMyTerminalBlock mapTerminalBlock)
            {
                var customData = mapTerminalBlock.CustomData;
                return mapTerminalBlock.CustomData
                    .Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList()
                    .FindAll(it => GpsMark.IsGpsMark(it))
                    .ConvertAll(it => GpsMark.FromString(it));
            }

        }
    }
}
