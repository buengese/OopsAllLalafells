using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Plugin;
using Dalamud.Hooking;

namespace OopsAllLalafells
{
    public class Plugin : IDalamudPlugin
    {
        private DalamudPluginInterface pluginInterface;

        private const uint CHARA_WINDOW_ACTOR_ID = 0xE0000000;

        private const int LALAFELL_RACE_ID = 3;
        private const int LALAFELL_CLAN_OFFSET = 6;

        private const int OFFSET_RENDER_TOGGLE = 0x104;

        public string Name => "Oops, All Lalafells!";

        private delegate IntPtr CharacterInitialize(IntPtr actorPtr, IntPtr customizeDataPtr);

        private delegate IntPtr FlagSlotUpdate(IntPtr actorPtr, uint slot, IntPtr equipData);

        private Hook<CharacterInitialize> charaInitHook;
        private Hook<FlagSlotUpdate> flagSlotUpdateHook;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
            
            var charaInitAddr = this.pluginInterface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ?? 48 8B D7 E8 ?? ?? ?? ?? 48 8B CF E8 ?? ?? ?? ?? 48 8B C7");
            PluginLog.Log($"Found Initialize sig: {charaInitAddr.ToInt64():X}");
            
            this.charaInitHook ??= new Hook<CharacterInitialize>(charaInitAddr, new CharacterInitialize(CharacterInitializeDetour));
            this.charaInitHook.Enable();
            
            var flagSlotUpdateHook = this.pluginInterface.TargetModuleScanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B DA 49 8B F0 48 8B F9 83 FA 0A");
            PluginLog.Log($"Found FlagSlotUpdate sig: {flagSlotUpdateHook.ToInt64():X}");

            this.flagSlotUpdateHook ??=
                new Hook<FlagSlotUpdate>(flagSlotUpdateHook, new FlagSlotUpdate(FlagSlotUpdateDetour));
            this.flagSlotUpdateHook.Enable();
            
            // Trigger an initial refresh of all players
            RefreshAllPlayers();
        }

        private IntPtr CharacterInitializeDetour(IntPtr actorPtr, IntPtr customizeDataPtr)
        {
            var customizeData = Marshal.PtrToStructure<CharaCustomizeData>(customizeDataPtr);

            customizeData.race = LALAFELL_RACE_ID;
            customizeData.clan = (byte) (LALAFELL_CLAN_OFFSET - customizeData.clan % 2);

            // Constrain face type to 0-3 so we don't decapitate the character
            customizeData.faceType %= 4;
            
            // Constrain body type to 0-1 so we don't crash the game
            customizeData.bodyType %= 2;
            
            Marshal.StructureToPtr(customizeData, customizeDataPtr, true);
            return charaInitHook.Original(actorPtr, customizeDataPtr);
        }

        private IntPtr FlagSlotUpdateDetour(IntPtr actorPtr, uint slot, IntPtr equipDataPtr)
        {
            var equipData = Marshal.PtrToStructure<EquipData>(equipDataPtr);
            equipData = MapRacialModelEquip(equipData);
            
            Marshal.StructureToPtr(equipData, equipDataPtr, true);
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

        private async void RerenderActor(Actor actor)
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

            this.charaInitHook.Disable();
            this.flagSlotUpdateHook.Disable();
            
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
