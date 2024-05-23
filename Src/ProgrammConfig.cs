
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
                InitializeCustomData(programmableBlock);

                PanelNameIdentifier = GetConfigField(programmableBlock, PanelIdentifierAttrName) ?? PanelIdentifierDefault;
                SearchLocalGridOnly = bool.Parse(GetConfigField(programmableBlock, SearchLocalGridAttrName) ?? SearchLocalGridDefault);
            }

            private void InitializeCustomData(IMyProgrammableBlock programmableBlock)
            {
                if (!programmableBlock.CustomData.Contains(SearchLocalGridAttrName))
                {
                    programmableBlock.CustomData = $"{SearchLocalGridAttrName}={SearchLocalGridDefault}\n\n" + programmableBlock.CustomData;
                }

                if (!programmableBlock.CustomData.Contains(PanelIdentifierAttrName))
                {
                    programmableBlock.CustomData = $"{PanelIdentifierAttrName}={PanelIdentifierDefault}\n\n" + programmableBlock.CustomData;
                }
            }

            private string GetConfigField(IMyProgrammableBlock programmableBlock, string AttrName)
            {
                return programmableBlock.CustomData
                    .Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .First(line => line.StartsWith(AttrName))
                    ?.Split('=')[1];
            }
        
        }
    }
}
