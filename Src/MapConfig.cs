
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

            private static string ScreenNumberAttrName = "ScreenNumber";
            private static string ScreenNumberDefault = "0";

            public int DetectionDistance { get; }
            public List<GpsMark> GpsMarks { get; }
            public int ScreenNumber { get; }

            public MapConfig(IMyTerminalBlock mapTerminalBlock)
            {
                DetectionDistance = int.Parse(GetConfigField(mapTerminalBlock, DetectionDistanceAttrName, DetectionDistanceDefault));
                GpsMarks = GetGPSList(mapTerminalBlock);
                if(mapTerminalBlock is IMyCockpit)
                {
                    ScreenNumber = int.Parse(GetConfigField(mapTerminalBlock, ScreenNumberAttrName, ScreenNumberDefault));
                }
            }

            private string GetConfigField(IMyTerminalBlock mapTerminalBlock, string attrName, string defaultValue)
            {
                if (!mapTerminalBlock.CustomData.Contains(attrName))
                {
                    mapTerminalBlock.CustomData = $"{attrName}={defaultValue}\n\n" + mapTerminalBlock.CustomData;
                }

                return mapTerminalBlock.CustomData
                    .Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .First(line => line.StartsWith(attrName))
                    ?.Split('=')[1]
                    ?? defaultValue;
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
