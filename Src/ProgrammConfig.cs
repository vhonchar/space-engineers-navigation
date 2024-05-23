
using Sandbox.ModAPI.Ingame;
using System;
using System.Linq;

namespace IngameScript
{
    partial class Program
    {
        public class ProgrammConfig
        {
            private static string PanelIdentifierAttrName = "PanelNameContains";
            private static string PanelIdentifierDefault = "GPS-Map";
            
            private static string SearchLocalGridAttrName = "LocalGridOnly";
            private static string SearchLocalGridDefault = "true";
            public string PanelNameIdentifier { get; }
            public bool SearchLocalGridOnly { get; }

            public ProgrammConfig(IMyProgrammableBlock programmableBlock)
            {
                PanelNameIdentifier = GetConfigField(programmableBlock, PanelIdentifierAttrName, PanelIdentifierDefault);
                SearchLocalGridOnly = bool.Parse(GetConfigField(programmableBlock, SearchLocalGridAttrName, SearchLocalGridDefault));
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

        }
    }
}
