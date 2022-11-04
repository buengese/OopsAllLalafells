#undef DEBUG

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Hooking;
using Dalamud.Logging;

namespace OopsAllLalafells
{
    public class Plugin : IDalamudPlugin
    {
        private const uint FlagInvis = (1 << 1) | (1 << 11);
        private const uint CharaWindowActorID = 0xE0000000;
        private const int OffsetRenderToggle = 0x104;

        private static readonly short[,] RaceStarterGearIDMap =
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

        private static readonly short[] RaceStarterGearIds;

        public string Name => "Oops, All Lalafells!";

        public Configuration Config { get; private set; }

        private bool _unsavedConfigChanges;

        private readonly PluginUI _ui;
        public bool SettingsVisible;

        private delegate IntPtr CharacterIsMount(IntPtr actor);

        private delegate IntPtr CharacterInitialize(IntPtr actorPtr, IntPtr customizeDataPtr);

        private delegate IntPtr FlagSlotUpdate(IntPtr actorPtr, uint slot, IntPtr equipData);

        private readonly Hook<CharacterIsMount> _charaMountedHook;
        private readonly Hook<CharacterInitialize> _charaInitHook;
        private readonly Hook<FlagSlotUpdate> _flagSlotUpdateHook;

        private IntPtr _lastActor;
        private bool _lastWasPlayer;
        private bool _lastWasModified;

        private Race _lastPlayerRace;
        private byte _lastPlayerGender;

        // This sucks, but here we are
        static Plugin()
        {
            var list = new List<short>();
            foreach (short id in RaceStarterGearIDMap)
            {
                if (id != -1)
                {
                    list.Add(id);
                }
            }

            RaceStarterGearIds = list.ToArray();
        }

        public Plugin(DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Service>();
            Service.Address = new PluginAddressResolver();
            Service.Address.Setup();

            this.Config = (Configuration?) pluginInterface.GetPluginConfig() ?? new Configuration();
            this.Config.Initialize(pluginInterface);

            this._ui = new PluginUI(this);

            Service.Interface.UiBuilder.Draw += this._ui.Draw;
            Service.Interface.UiBuilder.OpenConfigUi += OpenSettingsMenu;

            Service.CommandManager.AddHandler(
                "/poal",
                new CommandInfo(this.OpenSettingsMenuCommand)
                {
                    HelpMessage = "Opens the Oops, All Lalafells! settings menu.",
                    ShowInHelp = true
                }
            );


            this._charaMountedHook =
                Hook<CharacterIsMount>.FromAddress(Service.Address.CharacterIsMount, CharacterIsMountDetour);
            this._charaMountedHook.Enable();
            
            this._charaInitHook =
                Hook<CharacterInitialize>.FromAddress(Service.Address.CharacterInitialize, CharacterInitializeDetour);
            this._charaInitHook.Enable();
            
            this._flagSlotUpdateHook =
                Hook<FlagSlotUpdate>.FromAddress(Service.Address.FlagSlotUpdate, FlagSlotUpdateDetour);
            this._flagSlotUpdateHook.Enable();

            // Trigger an initial refresh of all players
            RefreshAllPlayers();
        }

        private IntPtr CharacterIsMountDetour(IntPtr actorPtr)
        {
            // TODO: use native FFXIVClientStructs unsafe methods?
            if (Marshal.ReadByte(actorPtr + 0x8C) == (byte) ObjectKind.Player)
            {
                _lastActor = actorPtr;
                _lastWasPlayer = true;
            }
            else
            {
                _lastWasPlayer = false;
            }

            return _charaMountedHook.Original(actorPtr);
        }

        private IntPtr CharacterInitializeDetour(IntPtr drawObjectBase, IntPtr customizeDataPtr)
        {
            if (_lastWasPlayer)
            {
                _lastWasModified = false;
                var actor = Service.ObjectTable.CreateObjectReference(_lastActor);
                if (actor != null &&
                    (actor.ObjectId != CharaWindowActorID || this.Config.ImmersiveMode)
                    && Service.ClientState.LocalPlayer != null
                    && actor.ObjectId != Service.ClientState.LocalPlayer.ObjectId
                    && this.Config.ShouldChangeOthers)
                {
                    this.ChangeRace(customizeDataPtr, this.Config.ChangeOthersTargetRace);
                }
            }

            return _charaInitHook.Original(drawObjectBase, customizeDataPtr);
        }

        private void ChangeRace(IntPtr customizeDataPtr, Race targetRace)
        {
            var customData = Marshal.PtrToStructure<CharaCustomizeData>(customizeDataPtr);

            if (customData.Race != targetRace)
            {
                // Modify the race/tribe accordingly
                customData.Race = targetRace;
                customData.Tribe = (byte) ((byte) customData.Race * 2 - customData.Tribe % 2);

                // Special-case Hrothgar gender to prevent fuckery
                customData.Gender = targetRace switch
                {
                    Race.Hrothgar => 0, // Force male for Hrothgar
                    _ => customData.Gender
                };

                // TODO: Re-evaluate these for valid race-specific values? (These are Lalafell values)
                // Constrain face type to 0-3 so we don't decapitate the character
                customData.FaceType %= 4;

                // Constrain body type to 0-1 so we don't crash the game
                customData.ModelType %= 2;

                // Hrothgar have a limited number of lip colors?
                customData.LipColor = targetRace switch
                {
                    Race.Hrothgar => (byte) (customData.LipColor % 5 + 1),
                    _ => customData.LipColor
                };

                customData.HairStyle = (byte) (customData.HairStyle % RaceMappings.RaceHairs[targetRace] + 1);

                Marshal.StructureToPtr(customData, customizeDataPtr, true);

                // Record the new race/gender for equip model mapping, and mark the equip as dirty
                _lastPlayerRace = customData.Race;
                _lastPlayerGender = customData.Gender;
                _lastWasModified = true;
            }
        }

        private IntPtr FlagSlotUpdateDetour(IntPtr actorPtr, uint slot, IntPtr equipDataPtr)
        {
            if (_lastWasPlayer && _lastWasModified)
            {
                var equipData = Marshal.PtrToStructure<EquipData>(equipDataPtr);
                // TODO: Handle gender-locked gear for Viera/Hrothgar
                equipData = MapRacialEquipModels(_lastPlayerRace, _lastPlayerGender, equipData);
                Marshal.StructureToPtr(equipData, equipDataPtr, true);
            }

            return _flagSlotUpdateHook.Original(actorPtr, slot, equipDataPtr);
        }

        public bool SaveConfig()
        {
            if (this._unsavedConfigChanges)
            {
                this.Config.Save();
                this._unsavedConfigChanges = false;
                this.RefreshAllPlayers();
                return true;
            }

            return false;
        }

        public void ToggleOtherRace(bool changeRace)
        {
            if (this.Config.ShouldChangeOthers == changeRace)
            {
                return;
            }

            PluginLog.Log($"Target race for other players toggled to {changeRace}, refreshing players");
            this.Config.ShouldChangeOthers = changeRace;
            _unsavedConfigChanges = true;
        }

        public void UpdateOtherRace(Race race)
        {
            if (this.Config.ChangeOthersTargetRace == race)
            {
                return;
            }

            PluginLog.Log($"Target race for other players changed to {race}, refreshing players");
            this.Config.ChangeOthersTargetRace = race;
            _unsavedConfigChanges = true;
        }

        public void UpdateImmersiveMode(bool immersiveMode)
        {
            if (this.Config.ImmersiveMode == immersiveMode)
            {
                return;
            }

            PluginLog.Log($"Immersive mode set to {immersiveMode}, refreshing players");
            this.Config.ImmersiveMode = immersiveMode;
            _unsavedConfigChanges = true;
        }

        private async void RefreshAllPlayers()
        {
            // Workaround to prevent literally genociding the actor table if we load at the same time as Dalamud + Dalamud is loading while ingame
            await Task.Delay(100); // LMFAOOOOOOOOOOOOOOOOOOO
            var localPlayer = Service.ClientState.LocalPlayer;
            if (localPlayer == null)
            {
                return;
            }

            for (var i = 0; i < Service.ObjectTable.Length; i++)
            {
                var actor = Service.ObjectTable[i];

                if (actor != null
                    && actor.ObjectKind == ObjectKind.Player)
                {
                    RerenderActor(actor);
                }
            }
        }

        private async void RerenderActor(GameObject actor)
        {
            try
            {
                var addrRenderToggle = actor.Address + OffsetRenderToggle;
                var val = Marshal.ReadInt32(addrRenderToggle);

                // Trigger a rerender
                val |= (int) FlagInvis;
                Marshal.WriteInt32(addrRenderToggle, val);
                await Task.Delay(100);
                val &= ~(int) FlagInvis;
                Marshal.WriteInt32(addrRenderToggle, val);
            }
            catch (Exception ex)
            {
                PluginLog.LogError(ex.ToString());
            }
        }

        private EquipData MapRacialEquipModels(Race race, int gender, EquipData eq)
        {
            if (Array.IndexOf(RaceStarterGearIds, eq.model) > -1)
            {
#if DEBUG
                PluginLog.Log($"Modified {eq.model}, {eq.variant}");
                PluginLog.Log($"Race {race}, index {(byte) (race - 1)}, gender {gender}");
#endif
                eq.model = RaceStarterGearIDMap[(byte) race - 1, gender];
                eq.variant = 1;
#if DEBUG
                PluginLog.Log($"New {eq.model}, {eq.variant}");
#endif
            }

            return eq;
        }

        public void OpenSettingsMenuCommand(string command, string args)
        {
            OpenSettingsMenu();
        }

        private void OpenSettingsMenu()
        {
            this.SettingsVisible = true;
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            Service.Interface.UiBuilder.OpenConfigUi -= OpenSettingsMenu;
            Service.Interface.UiBuilder.Draw -= this._ui.Draw;
            this.SaveConfig();

            this._charaMountedHook.Disable();
            this._charaInitHook.Disable();
            this._flagSlotUpdateHook.Disable();

            this._charaMountedHook.Dispose();
            this._charaInitHook.Dispose();
            this._flagSlotUpdateHook.Dispose();

            // Refresh all players again
            RefreshAllPlayers();

            Service.CommandManager.RemoveHandler("/poal");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}