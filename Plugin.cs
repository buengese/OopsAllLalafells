using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Structs;
using Dalamud.Plugin;
using Dalamud.Hooking;

namespace OopsAllLalafells
{
    public class Plugin : IDalamudPlugin
    {
        private DalamudPluginInterface pluginInterface;

        private const int ACTOR_DRAWOBJECT_OFFSET = 0xF0;
        private const uint CHARA_WINDOW_ACTOR_ID = 0xE0000000;

        private const int LALAFELL_RACE_ID = 3;
        private const int LALAFELL_CLAN_OFFSET = 6;

        private const int OFFSET_RENDER_TOGGLE = 0x104;

        public string Name => "Oops, All Lalafells!";

        private delegate IntPtr CharacterIsMounted(IntPtr actor);
        private delegate IntPtr CharacterInitialize(IntPtr actorPtr, IntPtr customizeDataPtr);
        private delegate IntPtr FlagSlotUpdate(IntPtr actorPtr, uint slot, IntPtr equipData);

        private Hook<CharacterIsMounted> charaMountedHook;
        private Hook<CharacterInitialize> charaInitHook;
        private Hook<FlagSlotUpdate> flagSlotUpdateHook;

        private IntPtr lastActor;
        private bool lastWasPlayer;
        private bool lastWasModified;
        
        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;

            var charaMountedAddr = this.pluginInterface.TargetModuleScanner.ScanText("48 83 EC 28 48 8B 01 FF 50 18 83 F8 08 0F 94 C0");
            PluginLog.Log($"Found IsMounted address: {charaMountedAddr.ToInt64():X}");
            this.charaMountedHook ??=
                new Hook<CharacterIsMounted>(charaMountedAddr, new CharacterIsMounted(CharacterIsMountedDetour));
            this.charaMountedHook.Enable();
            
            var charaInitAddr = this.pluginInterface.TargetModuleScanner.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 48 8B F9 48 8B EA 48 81 C1 ?? ?? ?? ?? E8 ?? ?? ?? ??");
            PluginLog.Log($"Found Initialize address: {charaInitAddr.ToInt64():X}");
            this.charaInitHook ??= new Hook<CharacterInitialize>(charaInitAddr, new CharacterInitialize(CharacterInitializeDetour));
            this.charaInitHook.Enable();
            
            var flagSlotUpdateAddr = this.pluginInterface.TargetModuleScanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B DA 49 8B F0 48 8B F9 83 FA 0A");
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
                var actor = Marshal.PtrToStructure<Dalamud.Game.ClientState.Structs.Actor>(lastActor);

                if ((uint) actor.ActorId != CHARA_WINDOW_ACTOR_ID)
                {
                    var customizeData = Marshal.PtrToStructure<CharaCustomizeData>(customizeDataPtr);

                    customizeData.race = LALAFELL_RACE_ID;
                    customizeData.clan = (byte) (LALAFELL_CLAN_OFFSET - customizeData.clan % 2);

                    // Constrain face type to 0-3 so we don't decapitate the character
                    customizeData.faceType %= 4;
            
                    // Constrain body type to 0-1 so we don't crash the game
                    customizeData.bodyType %= 2;
            
                    Marshal.StructureToPtr(customizeData, customizeDataPtr, true);
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
                equipData = MapRacialModelEquip(equipData);
            
                Marshal.StructureToPtr(equipData, equipDataPtr, true);
            }
            
            return flagSlotUpdateHook.Original(actorPtr, slot, equipDataPtr);
        }

        private void RefreshAllPlayers()
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
            await Task.Run(async () => {
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

        private EquipData MapRacialModelEquip(EquipData eq)
        {
            switch (eq.model)
            {
                // Male
                case 84: // Hyur
                case 86: // Elezen
                case 88: // Miqo
                case 90: // Roe
                case 257: // Au Ra
                case 597: // Hrothgar
                    eq.model = 92;
                    eq.variant = 1;
                    break;
                // Female
                case 85: // Hyur
                case 87: // Elezen
                case 89: // Miqo
                case 91: // Roe
                case 258: // Au Ra
                case 581: // Viera
                    eq.model = 93;
                    eq.variant = 1;
                    break;
            }

            return eq;
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

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
