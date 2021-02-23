#undef DEBUG

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Structs;
using Dalamud.Plugin;
using Dalamud.Hooking;
using OopsAllLalafells.Attributes;

namespace OopsAllLalafells {
    public class Plugin : IDalamudPlugin {
        private const uint CHARA_WINDOW_ACTOR_ID = 0xE0000000;

        private const int LALAFELL_RACE_ID = 3;

        private const int OFFSET_RENDER_TOGGLE = 0x104;

        private static readonly short[,] RACE_STARTER_GEAR_ID_MAP =
        {
            {84, 85}, // Hyur
            {86, 87}, // Elezen
            {92, 93}, // Lalafell
            {88, 89}, // Miqo
            {90, 91}, // Roe
            {257, 258}, // Au Ra
            {597, -1}, // Hrothgar
            {-1, 581}, // Viera
        };

        private static readonly short[] RACE_STARTER_GEAR_IDS;

        public string Name => "Oops, All Lalafells!";
        public DalamudPluginInterface pluginInterface { get; private set; }
        public Configuration config { get; private set; }
        private bool unsavedConfigChanges = false;

        private PluginUI ui;
        public bool SettingsVisible = false;

        private PluginCommandManager<Plugin> commandManager;

        private delegate IntPtr CharacterIsMounted(IntPtr actor);

        private delegate IntPtr CharacterInitialize(IntPtr actorPtr, IntPtr customizeDataPtr);

        private delegate IntPtr FlagSlotUpdate(IntPtr actorPtr, uint slot, IntPtr equipData);

        private Hook<CharacterIsMounted> charaMountedHook;
        private Hook<CharacterInitialize> charaInitHook;
        private Hook<FlagSlotUpdate> flagSlotUpdateHook;

        private IntPtr lastActor;
        private bool lastWasPlayer;
        private bool lastWasModified;

        private byte lastPlayerRace;
        private byte lastPlayerGender;

        // This sucks, but here we are
        static Plugin() {
            var list = new List<short>();
            foreach (short id in RACE_STARTER_GEAR_ID_MAP) {
                if (id != -1) {
                    list.Add(id);
                }
            }

            RACE_STARTER_GEAR_IDS = list.ToArray();
        }

        public void Initialize(DalamudPluginInterface pluginInterface) {
            this.pluginInterface = pluginInterface;

            this.config = (Configuration)this.pluginInterface.GetPluginConfig() ?? new Configuration();
            this.config.Initialize(pluginInterface);

            this.ui = new PluginUI(this, pluginInterface);

            this.pluginInterface.UiBuilder.OnBuildUi += this.ui.Draw;
            this.pluginInterface.UiBuilder.OnOpenConfigUi += OpenSettingsMenu;

            this.commandManager = new PluginCommandManager<Plugin>(this, this.pluginInterface);

            var charaMountedAddr =
                this.pluginInterface.TargetModuleScanner.ScanText("48 83 EC 28 48 8B 01 FF 50 18 83 F8 08 0F 94 C0");
            PluginLog.Log($"Found IsMounted address: {charaMountedAddr.ToInt64():X}");
            this.charaMountedHook ??=
                new Hook<CharacterIsMounted>(charaMountedAddr, new CharacterIsMounted(CharacterIsMountedDetour));
            this.charaMountedHook.Enable();

            var charaInitAddr = this.pluginInterface.TargetModuleScanner.ScanText(
                "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 48 8B F9 48 8B EA 48 81 C1 ?? ?? ?? ?? E8 ?? ?? ?? ??");
            PluginLog.Log($"Found Initialize address: {charaInitAddr.ToInt64():X}");
            this.charaInitHook ??=
                new Hook<CharacterInitialize>(charaInitAddr, new CharacterInitialize(CharacterInitializeDetour));
            this.charaInitHook.Enable();

            var flagSlotUpdateAddr =
                this.pluginInterface.TargetModuleScanner.ScanText(
                    "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B DA 49 8B F0 48 8B F9 83 FA 0A");
            PluginLog.Log($"Found FlagSlotUpdate address: {flagSlotUpdateAddr.ToInt64():X}");
            this.flagSlotUpdateHook ??=
                new Hook<FlagSlotUpdate>(flagSlotUpdateAddr, new FlagSlotUpdate(FlagSlotUpdateDetour));
            this.flagSlotUpdateHook.Enable();

            // Trigger an initial refresh of all players
            RefreshAllPlayers();
        }

        private IntPtr CharacterIsMountedDetour(IntPtr actorPtr) {
            if (Marshal.ReadByte(actorPtr + ActorOffsets.ObjectKind) == (byte)ObjectKind.Player) {
                lastActor = actorPtr;
                lastWasPlayer = true;
            } else {
                lastWasPlayer = false;
            }

            return charaMountedHook.Original(actorPtr);
        }

        private IntPtr CharacterInitializeDetour(IntPtr drawObjectBase, IntPtr customizeDataPtr) {
            if (lastWasPlayer)  {
                lastWasModified = false;
                var actor = Marshal.PtrToStructure<Actor>(lastActor);

                if ((uint)actor.ActorId != CHARA_WINDOW_ACTOR_ID
                    && this.pluginInterface.ClientState.LocalPlayer != null) {
                    bool isSelf = actor.ActorId == this.pluginInterface.ClientState.LocalPlayer.ActorId;
                    byte targetRace = isSelf ? this.config.SelfRace : this.config.OtherRace;
                    this.LogRace(customizeDataPtr, targetRace);
                    if (isSelf) {
                        if (this.config.SelfChange) {
                            this.ChangeRace(customizeDataPtr, this.config.SelfRace);
                        }
                    } else {
                        if (this.config.OtherChange) {
                            this.ChangeRace(customizeDataPtr, this.config.OtherRace);
                        }
                    }
                }
            }

            return charaInitHook.Original(drawObjectBase, customizeDataPtr);
        }

        private void LogRace(IntPtr customizeDataPtr, byte targetRace) {
//#if(DEBUG)
            var customData = Marshal.PtrToStructure<CharaCustomizeData>(customizeDataPtr);
            if (customData.Race == targetRace) {
                PluginLog.Log($"Existing hairStyle is {customData.HairStyle}");
            }
//#endif
        }

        private void ChangeRace(IntPtr customizeDataPtr, byte targetRace) {
            var customData = Marshal.PtrToStructure<CharaCustomizeData>(customizeDataPtr);

            if (customData.Race != targetRace) {
                // Modify the race/tribe accordingly
                customData.Race = targetRace;
                customData.Tribe = (byte)(customData.Race * 2 - customData.Tribe % 2);

                // Special-case Hrothgar/Viera gender to prevent fuckery
                customData.Gender = targetRace switch {
                    7 => 0, // Force male for Hrothgar
                    8 => 1, // Force female for Viera
                    _ => customData.Gender
                };

                // TODO: Re-evaluate these for valid race-specific values? (These are Lalafell values)
                // Constrain face type to 0-3 so we don't decapitate the character
                customData.FaceType %= 4;

                // Constrain body type to 0-1 so we don't crash the game
                customData.ModelType %= 2;

                // Hrothgar have a limited number of lip colors?
                customData.LipColor = targetRace switch {
                    7 => (byte) (customData.LipColor % 5 + 1),
                    _ => customData.LipColor
                };
                
                // TODO: Get values for other races
                customData.HairStyle = targetRace switch {
                    7 => (byte) (customData.HairStyle % 8 + 1), // Hrothgar cap at 7
                    8 => (byte) (customData.HairStyle % 17 + 1), // Viera cap at 17
                    _ => customData.LipColor
                };
                
                Marshal.StructureToPtr(customData, customizeDataPtr, true);

                // Record the new race/gender for equip model mapping, and mark the equip as dirty
                lastPlayerRace = customData.Race;
                lastPlayerGender = customData.Gender;
                lastWasModified = true;
#if(DEBUG)
                foreach (CustomizeIndex ndx in Enum.GetValues(typeof(CustomizeIndex))) {
                    PluginLog.Log($"Modified {ndx} is {Marshal.PtrToStructure<byte>(customizeDataPtr + (int)ndx)}");
                }
#endif
            }
        }

        private IntPtr FlagSlotUpdateDetour(IntPtr actorPtr, uint slot, IntPtr equipDataPtr) {
            if (lastWasPlayer) {
                // LogEquipModels(equipData, !lastWasModified);
                if (lastWasModified) {
                    var equipData = Marshal.PtrToStructure<EquipData>(equipDataPtr);
                    equipData = MapRacialEquipModels(lastPlayerRace, lastPlayerGender, equipData);
                    Marshal.StructureToPtr(equipData, equipDataPtr, true);
                }
            }

            return flagSlotUpdateHook.Original(actorPtr, slot, equipDataPtr);
        }

        private EquipData LogEquipModels(EquipData eq, bool correctRace) {
#if(DEBUG)
            if (correctRace) {
                PluginLog.Log($"Modified {eq.model}, {eq.variant}");
            } else {
                PluginLog.Log($"Existing {eq.model}, {eq.variant}");
            }
#endif
            return eq;
        }

        public bool SaveConfig() {
            if (this.unsavedConfigChanges) {
                this.config.Save();
                this.unsavedConfigChanges = false;
                this.RefreshAllPlayers();
                return true;
            }
            return false;
        }

        public void ToggleOtherRace(bool changeRace) {
            if (this.config.OtherChange == changeRace) {
                return;
            }

            PluginLog.Log($"OtherRace toggled to {changeRace}, refreshing players");
            this.config.OtherChange = changeRace;
            unsavedConfigChanges = true;
        }

        public void UpdateOtherRace(int id) {
            if (this.config.OtherRace == id) {
                return;
            }

            PluginLog.Log($"OtherRace changed to {id}, refreshing players");
            this.config.OtherRace = (byte)id;
            unsavedConfigChanges = true;
        }

        public void ToggleSelfRace(bool changeRace) {
            if (this.config.SelfChange == changeRace) {
                return;
            }

            PluginLog.Log($"SelfRace toggled to {changeRace}, refreshing players");
            this.config.SelfChange = changeRace;
            unsavedConfigChanges = true;
        }

        public void UpdateSelfRace(int id) {
            if (this.config.SelfRace == id) {
                return;
            }

            PluginLog.Log($"SelfRace changed to {id}, refreshing players");
            this.config.SelfRace = (byte)id;
            unsavedConfigChanges = true;
        }

        public void RefreshAllPlayers() {
            var localPlayer = this.pluginInterface.ClientState.LocalPlayer;
            if (localPlayer == null) {
                return;
            }

            for (var i = 0; i < this.pluginInterface.ClientState.Actors.Length; i++) {
                var actor = this.pluginInterface.ClientState.Actors[i];

                if (actor != null
                    && actor.ObjectKind == ObjectKind.Player) {
                    RerenderActor(actor);
                }
            }
        }

        private async void RerenderActor(Dalamud.Game.ClientState.Actors.Types.Actor actor) {
            try {
                var addrRenderToggle = actor.Address + OFFSET_RENDER_TOGGLE;

                // Trigger a rerender
                Marshal.WriteInt32(addrRenderToggle, 2);
                await Task.Delay(100);
                Marshal.WriteInt32(addrRenderToggle, 0);
            } catch (Exception ex) {
                PluginLog.LogError(ex.ToString());
            }
        }

        private EquipData MapRacialEquipModels(int race, int gender, EquipData eq) {
            if (Array.IndexOf(RACE_STARTER_GEAR_IDS, eq.model) > -1) {
                PluginLog.Log($"Modified {eq.model}, {eq.variant}");
                PluginLog.Log($"Race {race}, index {race - 1}, gender {gender}");
                eq.model = RACE_STARTER_GEAR_ID_MAP[race - 1, gender];
                eq.variant = 1;
                PluginLog.Log($"New {eq.model}, {eq.variant}");
            }

            return eq;
        }

        [Command("/poal")]
        [HelpMessage("Opens the Oops, All Lalafells! settings menu.")]
        public void OpenSettingsMenuCommand(string command, string args) {
            OpenSettingsMenu(command, args);
        }

        private void OpenSettingsMenu(object a, object b) {
            this.SettingsVisible = true;
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing) {
            if (!disposing) return;

            this.commandManager.Dispose();

            this.pluginInterface.UiBuilder.OnOpenConfigUi -= OpenSettingsMenu;
            this.pluginInterface.UiBuilder.OnBuildUi -= this.ui.Draw;
            this.SaveConfig();

            this.charaMountedHook.Disable();
            this.charaInitHook.Disable();
            this.flagSlotUpdateHook.Disable();

            this.charaMountedHook.Dispose();
            this.charaInitHook.Dispose();
            this.flagSlotUpdateHook.Dispose();

            // Refresh all players again
            RefreshAllPlayers();

            this.pluginInterface.Dispose();
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

}