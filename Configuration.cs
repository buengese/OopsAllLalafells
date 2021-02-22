using Dalamud.Configuration;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Numerics;

namespace OopsAllLalafells {
    public class Configuration : IPluginConfiguration {
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

        [NonSerialized]
        private DalamudPluginInterface pluginInterface;
        [NonSerialized]
        private Plugin plugin;

        public int Version { get; set; } = 1;
        public byte OtherRace { get; set; } = 3;
        public byte SelfRace { get; set; } = 3;
        public bool OtherChange { get; set; } = false;
        public bool SelfChange { get; set; } = false;
        [NonSerialized]
        public byte HairColor = 110;
        [NonSerialized]
        public byte HairColor2 = 2;
        [NonSerialized]
        public byte FaceType = 1;
        [NonSerialized]
        public byte HairStyle = 1;
        [NonSerialized]
        public byte BustSize = 0;
        [NonSerialized]
        public byte RaceFeatureSize = 100;
        [NonSerialized]
        public byte RaceFeatureType = 4;

        public void Initialize(Plugin plugin, DalamudPluginInterface pluginInterface) {
            this.plugin = plugin;
            this.pluginInterface = pluginInterface;
        }

        public bool SettingsVisible = false;

        public void Save() {
            this.pluginInterface.SavePluginConfig(this);
        }

        public void Draw() {
            if (!SettingsVisible) {
                return;
            }
            
            bool updated = false;
            {
                bool settingsVisible = this.SettingsVisible;
                ImGui.SetNextWindowSize(new Vector2(350, 400), ImGuiCond.FirstUseEver);
                if (ImGui.Begin("Oops, All Lalafells!", ref settingsVisible, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse) && settingsVisible != this.SettingsVisible) {
                    this.SettingsVisible = settingsVisible;
                    updated = true;
                }
            }
            {
                bool uiOtherChange = OtherChange;
                if (ImGui.Checkbox("Change others", ref uiOtherChange) && uiOtherChange != OtherChange) {
                    this.plugin.ToggleOtherRace(uiOtherChange);
                    updated = true;
                }
                if (uiOtherChange) {
                    int uiOtherRaceIndex = this.OtherRace - 1;
                    if (ImGui.Combo("Target Race", ref uiOtherRaceIndex, RACE_NAME, RACE_NAME.Length) && uiOtherRaceIndex + 1 != this.OtherRace) {
                        this.plugin.UpdateOtherRace(uiOtherRaceIndex + 1);
                        updated = true;
                    }
                }
            }

            ImGui.Spacing();
            {
                bool uiSelfChange = SelfChange;
                if (ImGui.Checkbox("Change self", ref uiSelfChange) && uiSelfChange != SelfChange) {
                    this.plugin.ToggleSelfRace(uiSelfChange);
                    updated = true;
                }
                if (uiSelfChange) {
                    int uiSelfRaceIndex = this.SelfRace - 1;
                    if (ImGui.Combo("Self Race", ref uiSelfRaceIndex, RACE_NAME, RACE_NAME.Length) && uiSelfRaceIndex + 1 != SelfRace) {
                        this.plugin.UpdateSelfRace(uiSelfRaceIndex + 1);
                        updated = true;
                    }
                }
            }

            ImGui.Spacing();
            {
                string uiColor = HairColor.ToString();
                if (ImGui.InputText("Hair Color", ref uiColor, 3) && !uiColor.Equals(HairColor.ToString()))
                {
                    this.HairColor = Byte.Parse(uiColor);
                    this.plugin.RefreshAllPlayers();
                }
            }

            ImGui.Spacing();
            {
                string uiColor2 = HairColor2.ToString();
                if (ImGui.InputText("Hair Color 2", ref uiColor2, 3) && !uiColor2.Equals(HairColor2.ToString()))
                {
                    this.HairColor2 = Byte.Parse(uiColor2);
                    this.plugin.RefreshAllPlayers();
                }
            }

            ImGui.Spacing();
            {
                string uiFace = FaceType.ToString();
                if (ImGui.InputText("Face Type", ref uiFace, 1) && !uiFace.Equals(FaceType.ToString()))
                {
                    this.FaceType = Byte.Parse(uiFace);
                    this.plugin.RefreshAllPlayers();
                }
            }

            ImGui.Spacing();
            {
                string uiHair = FaceType.ToString();
                if (ImGui.InputText("Hair Style", ref uiHair, 1) && !uiHair.Equals(HairStyle.ToString()))
                {
                    this.HairStyle = Byte.Parse(uiHair);
                    this.plugin.RefreshAllPlayers();
                }
            }

            ImGui.Spacing();
            {
                string uiBust = BustSize.ToString();
                if (ImGui.InputText("Bust Size", ref uiBust, 3) && !uiBust.Equals(BustSize.ToString()))
                {
                    this.BustSize = Byte.Parse(uiBust);
                    this.plugin.RefreshAllPlayers();
                }
            }

            ImGui.Spacing();
            {
                string uiRaceSize = RaceFeatureSize.ToString();
                if (ImGui.InputText("Race Feature Size", ref uiRaceSize, 3) && !uiRaceSize.Equals(RaceFeatureSize.ToString()))
                {
                    this.RaceFeatureSize = Byte.Parse(uiRaceSize);
                    this.plugin.RefreshAllPlayers();
                }
            }

            ImGui.Spacing();
            {
                string uiRaceType = RaceFeatureType.ToString();
                if (ImGui.InputText("Race Feature Type", ref uiRaceType, 3) && !uiRaceType.Equals(RaceFeatureType.ToString()))
                {
                    this.RaceFeatureType = Byte.Parse(uiRaceType);
                    this.plugin.RefreshAllPlayers();
                }
            }

            if (updated) {
                Save();
            }

            ImGui.End();
        }
    }
}
