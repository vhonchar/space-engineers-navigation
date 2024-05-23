
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

            private readonly IMyProgrammableBlock programmableBlock;

            public ProgrammConfig(IMyProgrammableBlock programmableBlock)
            {
                this.programmableBlock = programmableBlock;
                InitializeCustomData();
            }

            private void InitializeCustomData()
            {
                if (!programmableBlock.CustomData.Contains(PanelIdentifierAttrName))
                {
                    programmableBlock.CustomData = $"{PanelIdentifierAttrName}={PanelIdentifierDefault}\n\n" + programmableBlock.CustomData;
                }
            }

            public string PanelNameIdentifier {
                get {
                    return programmableBlock.CustomData
                    .Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .First(line => line.StartsWith(PanelIdentifierAttrName))
                    ?.Split('=')[1]
                    ?? PanelIdentifierDefault;
                }
            }
        
        }
    }
}
