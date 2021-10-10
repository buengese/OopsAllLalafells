using ImGuiNET;
using System;
using System.Numerics;
using Dalamud.Utility;

namespace OopsAllLalafells
{
    public class PluginUI
    {
        private static Vector4 WHAT_THE_HELL_ARE_YOU_DOING = new Vector4(1, 0, 0, 1);
        private readonly Plugin plugin;
        private bool enableExperimental;

        public PluginUI(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void Draw()
        {
            if (!this.plugin.SettingsVisible)
            {
                return;
            }

            bool settingsVisible = this.plugin.SettingsVisible;
            if (ImGui.Begin("Oops, All Lalafells!", ref settingsVisible, ImGuiWindowFlags.AlwaysAutoResize))
            {
                bool shouldChangeOthers = this.plugin.config.ShouldChangeOthers;
                ImGui.Checkbox("Change other players", ref shouldChangeOthers);
                
                if (enableExperimental)
                {
                    Race othersTargetRace = this.plugin.config.ChangeOthersTargetRace;
                    if (shouldChangeOthers)
                    {
                        if (ImGui.BeginCombo("Race", othersTargetRace.GetAttribute<Display>().Value))
                        {
                            foreach (Race race in Enum.GetValues(typeof(Race)))
                            {
                                ImGui.PushID((byte) race);
                                if (ImGui.Selectable(race.GetAttribute<Display>().Value, race == othersTargetRace))
                                {
                                    othersTargetRace = race;
                                }

                                if (race == othersTargetRace)
                                {
                                    ImGui.SetItemDefaultFocus();
                                }

                                ImGui.PopID();
                            }

                            ImGui.EndCombo();
                        }
                    }
                    
                    this.plugin.UpdateOtherRace(othersTargetRace);
                }
                else
                {
                    this.plugin.UpdateOtherRace(Race.LALAFELL);
                }
                
                this.plugin.ToggleOtherRace(shouldChangeOthers);
                
                ImGui.Separator();
                
                ImGui.Checkbox("Enable Experimental Features", ref this.enableExperimental);
                if (enableExperimental)
                {
                    ImGui.Text("Experimental feature configuration will (intentionally) not persist,\n" +
                               "so you will need to open this settings menu to re-activate\n" +
                               "them if you disable the plugin or restart your game.");

                    ImGui.TextColored(WHAT_THE_HELL_ARE_YOU_DOING,
                        "Experimental features may crash your game, uncat your boy,\nor cause the Eighth Umbral Calamity. YOU HAVE BEEN WARNED!");
                    
                    ImGui.Text("But seriously, if you do encounter any crashes, please report\nthem to Avaflow#0001 on Discord with whatever details you can get.");
                }

                ImGui.End();
            }

            this.plugin.SettingsVisible = settingsVisible;
            this.plugin.SaveConfig();
        }
    }
}