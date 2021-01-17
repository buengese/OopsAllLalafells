using System.Numerics;
using ImGuiNET;

namespace OopsAllLalafells
{
    public class PluginUI
    {
        private readonly Plugin plugin;

        public PluginUI(Plugin plugin)
        {
            this.plugin = plugin;
            this.targetRaceIndex = this.plugin.Config.TargetRace - 1;
        }

        private static readonly string[] RACE_NAME =
        {
            "Hyur",
            "Elezen",
            "Lalafell",
            "Miqo'te",
            "Roegadyn",
            "Au Ra",
            "Hrothgar",
            "Viera"
        };
        
        public bool IsVisible { get; set; }

        public int TargetRace => targetRaceIndex;
        private int targetRaceIndex = 2;

        public void Draw()
        {
            plugin.UpdateTargetRace(this.targetRaceIndex + 1);
            
            if (!IsVisible)
            {
                return;
            }
            
            // ImGui.SetNextWindowSize(new Vector2(500, 500), ImGuiCond.Always);

            bool isOpen = true;
            if (!ImGui.Begin("Oops, All Lalafells!", ref isOpen, ImGuiWindowFlags.NoCollapse))
            {
                ImGui.End();
                return;
            }

            ImGui.Combo("Target Race", ref this.targetRaceIndex, RACE_NAME, RACE_NAME.Length);
            
            ImGui.End();
        }
    }
}