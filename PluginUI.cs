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

        public CustomizeIndex customizeIndex = CustomizeIndex.HairStyle;
        public byte customValue = 1;

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
            Race uiOtherRace = this.plugin.config.OtherRace;
            bool uiSelfChange = this.plugin.config.SelfChange;
            Race uiSelfRace= this.plugin.config.SelfRace;
            ImGui.SetNextWindowSize(new Vector2(350, 400), ImGuiCond.FirstUseEver);
            ImGui.Begin("Oops, All Lalafells!", ref settingsVisible, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse);

            ImGui.Checkbox("Change others", ref uiOtherChange);
            if (uiOtherChange) {
                if (ImGui.BeginCombo("Others Race", uiOtherRace.ToString())) {
                    foreach (Race race in Enum.GetValues(typeof(Race))) {
                        ImGui.PushID((byte) race);
                        if (ImGui.Selectable(race.ToString(), race == uiOtherRace)) {
                            uiOtherRace = race;
                        }
                        if (race == uiOtherRace) {
                            ImGui.SetItemDefaultFocus();
                        }
                        ImGui.PopID();
                    }

                    ImGui.EndCombo();
                }
            }

            ImGui.Spacing();
            ImGui.Checkbox("Change self", ref uiSelfChange);
            if (uiSelfChange) {
                if (ImGui.BeginCombo("Self Race", uiSelfRace.ToString())) {
                    foreach (Race race in Enum.GetValues(typeof(Race))) {
                        ImGui.PushID((byte) race);
                        if (ImGui.Selectable(race.ToString(), race == uiSelfRace)) {
                            uiSelfRace = race;
                        }
                        if (race == uiSelfRace) {
                            ImGui.SetItemDefaultFocus();
                        }
                        ImGui.PopID();
                    }

                    ImGui.EndCombo();
                }
            }

            this.plugin.SettingsVisible = settingsVisible;
            this.plugin.ToggleOtherRace(uiOtherChange);
            this.plugin.UpdateOtherRace(uiOtherRace);
            this.plugin.ToggleSelfRace(uiSelfChange);
            this.plugin.UpdateSelfRace(uiSelfRace);

            bool tempUpdated = false;
            ImGui.Spacing();

            ImGuiComboFlags comboFlags = 0;
            // comboFlags |= ImGuiComboFlags.NoPreview;
            if (ImGui.BeginCombo("Fixed Attribute", Enum.GetName(typeof(CustomizeIndex), customizeIndex), comboFlags)) {
                foreach (CustomizeIndex opt in Enum.GetValues(typeof(CustomizeIndex))) {
                    ImGui.PushID((int) opt);
                    if (ImGui.Selectable(opt.ToString(), opt == customizeIndex)) {
                        if (opt != customizeIndex) {
                            customizeIndex = opt;
                            tempUpdated = true;
                        }
                    }
                    if (opt == customizeIndex) {
                        ImGui.SetItemDefaultFocus();
                    }
                    ImGui.PopID();
                }
                ImGui.EndCombo();
            }

            ImGui.Spacing();
            {
                string uiCustom = this.customValue.ToString();
                ImGui.InputText(customizeIndex.ToString(), ref uiCustom, 3);
                if (!uiCustom.Equals(this.customValue.ToString())) {
                    this.customValue = parseByte(uiCustom, this.customValue);
                    tempUpdated = true;
                }
            }
            /*
            ImGui.Spacing();
            {
                string uiColor = this.customizeData.LipColor.ToString();
                ImGui.InputText("Lip Color", ref uiColor, 3);
                if (!uiColor.Equals(this.customizeData.LipColor.ToString())) {
                    this.customizeData.LipColor = parseByte(uiColor, this.customizeData.LipColor);
                    tempUpdated = true;
                }
            }

            ImGui.Spacing();
            {
                string uiColor = this.customizeData.HairColor.ToString();
                ImGui.InputText("Hair Color", ref uiColor, 3);
                if (!uiColor.Equals(this.customizeData.HairColor.ToString()))
                {
                    this.customizeData.HairColor = parseByte(uiColor, this.customizeData.HairColor);
                    tempUpdated = true;
                }
            }

            ImGui.Spacing();
            {
                string uiColor2 = this.customizeData.HairColor2.ToString();
                ImGui.InputText("Hair Color 2", ref uiColor2, 3);
                if (!uiColor2.Equals(this.customizeData.HairColor2.ToString())) {
                    this.customizeData.HairColor2 = parseByte(uiColor2, this.customizeData.HairColor2);
                    tempUpdated = true;
                }
            }

            ImGui.Spacing();
            {
                string uiFace = this.customizeData.FaceType.ToString();
                ImGui.InputText("Face Type", ref uiFace, 1);
                if (!uiFace.Equals(this.customizeData.FaceType.ToString())) {
                    this.customizeData.FaceType = parseByte(uiFace, this.customizeData.FaceType);
                    tempUpdated = true;
                }
            }

            ImGui.Spacing();
            {
                string uiBust = this.customizeData.BustSize.ToString();
                ImGui.InputText("Bust Size", ref uiBust, 3);
                if (!uiBust.Equals(this.customizeData.BustSize.ToString())) {
                    this.customizeData.BustSize = parseByte(uiBust, this.customizeData.BustSize);
                    tempUpdated = true;
                }
            }

            ImGui.Spacing();
            {
                string uiRaceSize = this.customizeData.RaceFeatureSize.ToString();
                ImGui.InputText("Race Feature Size", ref uiRaceSize, 3);
                if (!uiRaceSize.Equals(this.customizeData.RaceFeatureSize.ToString())) {
                    this.customizeData.RaceFeatureSize = parseByte(uiRaceSize, this.customizeData.RaceFeatureSize);
                    tempUpdated = true;
                }
            }

            ImGui.Spacing();
            {
                string uiRaceType = this.customizeData.RaceFeatureType.ToString();
                ImGui.InputText("Race Feature Type", ref uiRaceType, 3);
                if (!uiRaceType.Equals(this.customizeData.RaceFeatureType.ToString())) {
                    this.customizeData.RaceFeatureType = parseByte(uiRaceType, this.customizeData.RaceFeatureType);
                    tempUpdated = true;
                }
            }
            */
            ImGui.End();

            bool configSaved = this.plugin.SaveConfig();
            if (!configSaved && tempUpdated) {
                this.plugin.RefreshAllPlayers();
            }
        }

        static byte parseByte(string val, byte current) {
            if (val.Length == 0) {
                return current;
            }
            return Byte.Parse(val);
        }
    }
}
