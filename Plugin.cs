using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Structs;
using Dalamud.Plugin;
using Dalamud.Hooking;
using OopsAllLalafells.Attributes;

namespace OopsAllLalafells
{
    public class Plugin : IDalamudPlugin
    {
        private DalamudPluginInterface pluginInterface;

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
        private Configuration config;
        private PluginUI ui;
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

        public Configuration Config => this.config;

        // This sucks, but here we are
        static Plugin()
        {
            var list = new List<short>();
            foreach (short id in RACE_STARTER_GEAR_ID_MAP)
            {
                if (id != -1)
                {
                    list.Add(id);
                }
            }

            RACE_STARTER_GEAR_IDS = list.ToArray();
        }

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;

            this.config = (Configuration) this.pluginInterface.GetPluginConfig() ?? new Configuration();
            this.config.Initialize(pluginInterface);

            this.ui = new PluginUI(this);
            this.pluginInterface.UiBuilder.OnBuildUi += this.ui.Draw;

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

        private IntPtr CharacterIsMountedDetour(IntPtr actorPtr)
        {
            if (Marshal.ReadByte(actorPtr + ActorOffsets.ObjectKind) == (byte) ObjectKind.Player)
            {
                lastActor = actorPtr;
                lastWasPlayer = true;
            }
            else
            {
                lastWasPlayer = false;
            }

            return charaMountedHook.Original(actorPtr);
        }

        private IntPtr CharacterInitializeDetour(IntPtr drawObjectBase, IntPtr customizeDataPtr)
        {
            if (lastWasPlayer)
            {
                var actor = Marshal.PtrToStructure<Actor>(lastActor);

                if ((uint) actor.ActorId != CHARA_WINDOW_ACTOR_ID)
                {
                    var customizeData = Marshal.PtrToStructure<CharaCustomizeData>(customizeDataPtr);

                    // Modify the race/clan accordingly
                    customizeData.race = this.config.TargetRace;
                    customizeData.clan = (byte) (customizeData.race * 2 - customizeData.clan % 2);

                    // Specialcase Hrothgar/Viera gender to prevent fuckery
                    customizeData.gender = this.config.TargetRace switch
                    {
                        7 => 0, // Force male for Hrothgar
                        8 => 1, // Force female for Viera
                        _ => customizeData.gender
                    };

                    // TODO: Re-evaluate these for valid race-specific values? (These are Lalafell values)
                    // Constrain face type to 0-3 so we don't decapitate the character
                    customizeData.faceType %= 4;

                    // Constrain body type to 0-1 so we don't crash the game
                    customizeData.bodyType %= 2;

                    Marshal.StructureToPtr(customizeData, customizeDataPtr, true);
                    
                    // Record the new race/gender for equip model mapping, and mark the equip as dirty
                    lastPlayerRace = customizeData.race;
                    lastPlayerGender = customizeData.gender;
                    lastWasModified = true;
                }
                else
                {
                    lastWasModified = false;
                }
            }

            return charaInitHook.Original(drawObjectBase, customizeDataPtr);
        }

        private IntPtr FlagSlotUpdateDetour(IntPtr actorPtr, uint slot, IntPtr equipDataPtr)
        {
            if (lastWasPlayer && lastWasModified)
            {
                var equipData = Marshal.PtrToStructure<EquipData>(equipDataPtr);
                equipData = MapRacialEquipModels(lastPlayerRace, lastPlayerGender, equipData);

                Marshal.StructureToPtr(equipData, equipDataPtr, true);
            }

            return flagSlotUpdateHook.Original(actorPtr, slot, equipDataPtr);
        }

        public void UpdateTargetRace(int id)
        {
            if (this.config.TargetRace == id)
            {
                return;
            }

            PluginLog.Log($"TargetRace changed to {id}, refreshing players");
            this.config.TargetRace = (byte) id;
            RefreshAllPlayers();
        }

        public void RefreshAllPlayers()
        {
            var localPlayer = this.pluginInterface.ClientState.LocalPlayer;
            if (localPlayer == null)
            {
                return;
            }

            for (var i = 0; i < this.pluginInterface.ClientState.Actors.Length; i++)
            {
                var actor = this.pluginInterface.ClientState.Actors[i];

                if (actor != null && actor.ObjectKind == ObjectKind.Player)
                {
                    RerenderActor(actor);
                }
            }
        }

        private async void RerenderActor(Dalamud.Game.ClientState.Actors.Types.Actor actor)
        {
            await Task.Run(async () =>
            {
                try
                {
                    var addrRenderToggle = actor.Address + OFFSET_RENDER_TOGGLE;

                    // Trigger a rerender
                    Marshal.WriteInt32(addrRenderToggle, 2);
                    await Task.Delay(100);
                    Marshal.WriteInt32(addrRenderToggle, 0);
                }
                catch (Exception ex)
                {
                    PluginLog.LogError(ex.ToString());
                }
            });
        }

        private EquipData MapRacialEquipModels(int race, int gender, EquipData eq)
        {
            if (Array.IndexOf(RACE_STARTER_GEAR_IDS, eq.model) > -1)
            {
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
        public void OpenSettingsMenuCommand(string command, string args)
        {
            this.ui.IsVisible = true;
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            this.commandManager.Dispose();

            this.pluginInterface.UiBuilder.OnBuildUi -= this.ui.Draw;
            this.pluginInterface.SavePluginConfig(this.config);

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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}