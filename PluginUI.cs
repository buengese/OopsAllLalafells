using Dalamud.Plugin;
using ImGuiNET;
using Dalamud.Game.ClientState.Actors;
using System;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace OopsAllLalafells {
    public class PluginUI {
        private static readonly string[] RACE_NAME = {
            "Hyur",
            "Elezen",
            "Lalafell",
            "Miqo'te",
            "Roegadyn",
            "Au Ra",
            "Hrothgar",
            "Viera"
        };

        private readonly DalamudPluginInterface pluginInterface;
        private readonly Plugin plugin;

        public PluginUI(Plugin plugin, DalamudPluginInterface pluginInterface) {
            this.plugin = plugin;
            this.pluginInterface = pluginInterface;
        }

        public void Draw() {
            if (!this.plugin.SettingsVisible) {
                return;
            }

            bool settingsVisible = this.plugin.SettingsVisible;
            bool uiOtherChange = this.plugin.config.OtherChange;
            int uiOtherRaceIndex = this.plugin.config.OtherRace - 1;
            bool uiSelfChange = this.plugin.config.SelfChange;
            int uiSelfRaceIndex = this.plugin.config.SelfRace - 1;
            ImGui.SetNextWindowSize(new Vector2(350, 400), ImGuiCond.FirstUseEver);
            ImGui.Begin("Oops, All Lalafells!", ref settingsVisible, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse);

            ImGui.Checkbox("Change others", ref uiOtherChange);
            if (uiOtherChange) {
                ImGui.Combo("Target Race", ref uiOtherRaceIndex, RACE_NAME, RACE_NAME.Length);
            }

            ImGui.Spacing();
            ImGui.Checkbox("Change self", ref uiSelfChange);
            if (uiSelfChange) {
                ImGui.Combo("Self Race", ref uiSelfRaceIndex, RACE_NAME, RACE_NAME.Length);
            }

            this.plugin.SettingsVisible = settingsVisible;
            this.plugin.ToggleOtherRace(uiOtherChange);
            this.plugin.UpdateOtherRace(uiOtherRaceIndex + 1);
            this.plugin.ToggleSelfRace(uiSelfChange);
            this.plugin.UpdateSelfRace(uiSelfRaceIndex + 1);

            ImGui.End();

            this.plugin.SaveConfig();
        }
    }
}
