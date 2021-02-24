using ImGuiNET;
using System;
using Dalamud.Game.Chat;

namespace OopsAllLalafells {
    public class PluginUI {
        private readonly Plugin plugin;

        public PluginUI(Plugin plugin) {
            this.plugin = plugin;
        }

        public void Draw() {
            if (!this.plugin.SettingsVisible) {
                return;
            }

            bool settingsVisible = this.plugin.SettingsVisible;
            if (ImGui.Begin("Oops, All Lalafells!", ref settingsVisible, ImGuiWindowFlags.AlwaysAutoResize)) {
                bool uiSelfChange = this.plugin.config.SelfChange;
                Race uiSelfRace= this.plugin.config.SelfRace;
                DrawRaceOptions("Self", ref uiSelfChange, ref uiSelfRace);
                this.plugin.ToggleSelfRace(uiSelfChange);
                this.plugin.UpdateSelfRace(uiSelfRace);
                
                ImGui.Spacing();
                
                bool uiOtherChange = this.plugin.config.OtherChange;
                Race uiOtherRace = this.plugin.config.OtherRace;
                DrawRaceOptions("Others", ref uiOtherChange, ref uiOtherRace);
                this.plugin.ToggleOtherRace(uiOtherChange);
                this.plugin.UpdateOtherRace(uiOtherRace);

                ImGui.End();
            }
            this.plugin.SettingsVisible = settingsVisible;
            this.plugin.SaveConfig();
        }

        private static void DrawRaceOptions(string target, ref bool doChange, ref Race selectedRace) {
            ImGui.Checkbox("Change " + target, ref doChange);
            if (doChange) {
                if (ImGui.BeginCombo(target + " Race", selectedRace.GetAttribute<Display>().Value)) {
                    foreach (Race race in Enum.GetValues(typeof(Race))) {
                        ImGui.PushID((byte) race);
                        if (ImGui.Selectable(race.GetAttribute<Display>().Value, race == selectedRace)) {
                            selectedRace = race;
                        }

                        if (race == selectedRace) {
                            ImGui.SetItemDefaultFocus();
                        }

                        ImGui.PopID();
                    }

                    ImGui.EndCombo();
                }
            }
        }
    }
}
