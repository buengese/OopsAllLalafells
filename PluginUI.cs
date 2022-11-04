using ImGuiNET;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Utility;

namespace OopsAllLalafells
{
    public class PluginUI
    {
        private static readonly Vector4 WHAT_THE_HELL_ARE_YOU_DOING = new Vector4(1, 0, 0, 1);
        private readonly Plugin _plugin;
        private bool _enableExperimental;

        // had to do it
        private bool _changeSelf;
        private bool _changeSelfLaunched;
        private bool _changeSelfShowText;

        public PluginUI(Plugin plugin)
        {
            this._plugin = plugin;
        }

        public void Draw()
        {
            if (!this._plugin.SettingsVisible)
            {
                return;
            }

            bool settingsVisible = this._plugin.SettingsVisible;
            if (ImGui.Begin("Oops, All Lalafells!", ref settingsVisible, ImGuiWindowFlags.AlwaysAutoResize))
            {
                bool shouldChangeOthers = this._plugin.Config.ShouldChangeOthers;
                ImGui.Checkbox("Change other players", ref shouldChangeOthers);

                Race othersTargetRace = this._plugin.Config.ChangeOthersTargetRace;
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

                this._plugin.UpdateOtherRace(othersTargetRace);
                this._plugin.ToggleOtherRace(shouldChangeOthers);

                ImGui.Checkbox("Change self", ref this._changeSelf);
                if (_changeSelf)
                {
                    if (!_changeSelfLaunched)
                    {
                        _changeSelfLaunched = true;
                        Process.Start("explorer", "https://store.finalfantasyxiv.com/ffxivstore/en-us/product/1");
                        TriggerChangeSelfText();
                    }

                    if (_changeSelfShowText)
                    {
                        ImGui.TextColored(WHAT_THE_HELL_ARE_YOU_DOING,
                            "Changing your own character's race/gender is not, and will never, be\na feature of this plugin. " +
                            "This is a policy held by both myself as the developer,\nas well as by the XIVLauncher/Dalamud folks. Sorry to be the bearer of bad news!");
                    }
                }

                if (_enableExperimental)
                {
                    bool immersiveMode = this._plugin.Config.ImmersiveMode;
                    ImGui.Checkbox("Immersive Mode", ref immersiveMode);
                    ImGui.Text("If Immersive Mode is enabled, \"Examine\" windows will also be modified.");

                    this._plugin.UpdateImmersiveMode(immersiveMode);
                }

                ImGui.Separator();

                ImGui.Checkbox("Enable Experimental Features", ref this._enableExperimental);
                if (_enableExperimental)
                {
                    ImGui.Text("Experimental feature configuration will (intentionally) not persist,\n" +
                               "so you will need to open this settings menu to re-activate\n" +
                               "them if you disable the plugin or restart your game.");

                    ImGui.TextColored(WHAT_THE_HELL_ARE_YOU_DOING,
                        "Experimental features may crash your game, uncat your boy,\nor cause the Eighth Umbral Calamity. YOU HAVE BEEN WARNED!");

                    ImGui.Text(
                        "But seriously, if you do encounter any crashes, please report\nthem to Avaflow#0001 on Discord with whatever details you can get.");
                }

                ImGui.End();
            }

            this._plugin.SettingsVisible = settingsVisible;
            this._plugin.SaveConfig();
        }

        private async void TriggerChangeSelfText()
        {
            await Task.Delay(2000);
            _changeSelfShowText = true;
        }
    }
}