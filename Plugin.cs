using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.Internal;
using Dalamud.Plugin;

namespace OopsAllLalafells
{
    public class Plugin : IDalamudPlugin
    {
        private DalamudPluginInterface pluginInterface;

        private const uint CHARA_WINDOW_ACTOR_ID = 0xE0000000;

        private const int LALAFELL_RACE_ID = 3;
        private const int LALAFELL_CLAN_OFFSET = 5;

        private const int OFFSET_RACE = 0x1878;
        private const int OFFSET_CLAN = 0x187C;
        private const int OFFSET_RENDER_TOGGLE = 0x104;
        private const int OFFSET_MODEL_TYPE = 0x1B4;

        private const int OFFSET_CHEST = 0x1044;
        private const int OFFSET_ARMS = 0x1048;
        private const int OFFSET_LEGS = 0x104C;
        private const int OFFSET_FEET = 0x1050;

        public string Name => "Oops, All Lalafells!";

#if DEBUG
        private int renderCycle = 0;
#endif

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;

            this.pluginInterface.Framework.OnUpdateEvent += RenderActors;
        }

        private void RenderActors(Framework framework)
        {
            var localPlayer = this.pluginInterface.ClientState.LocalPlayer;
            if (localPlayer == null)
            {
                return;
            }

            for (var k = 0; k < this.pluginInterface.ClientState.Actors.Length; k++)
            {
                var actor = this.pluginInterface.ClientState.Actors[k];

                if (actor == null
                    || actor.ObjectKind != ObjectKind.Player
                    || actor.ActorId == localPlayer.ActorId
                    || (uint) actor.ActorId == CHARA_WINDOW_ACTOR_ID)
                {
                    continue;
                }

                RerenderActor(actor);
            }

#if DEBUG
            renderCycle++;
#endif
        }

        private async void RerenderActor(Dalamud.Game.ClientState.Actors.Types.Actor a)
        {
            await Task.Run(async () => {
                try
                {
                    var addrRace = a.Address + OFFSET_RACE;
                    var addrClan = a.Address + OFFSET_CLAN;
                    var addrRenderToggle = a.Address + OFFSET_RENDER_TOGGLE;
                    var addrModelType = a.Address + OFFSET_MODEL_TYPE;

                    // Allow for compatibility with other plugins that may modify character models
                    int modelType = Marshal.ReadInt32(addrModelType, 0);
                    if (modelType != 0)
                    {
#if DEBUG
                        if (renderCycle % 300 == 0)
                        {
                            PluginLog.Log("Skipping invalid actor: modelType {0}, name {1} (distance: x {2} yalms, y {3} yalms)", modelType, a.Name, a.YalmDistanceX, a.YalmDistanceY);
                        }
#endif
                        return;
                    }

                    byte currentRace = Marshal.ReadByte(addrRace, 0);
                    if (currentRace != LALAFELL_RACE_ID)
                    {
                        Marshal.WriteByte(addrRace, LALAFELL_RACE_ID);

                        byte currentClan = Marshal.ReadByte(addrClan, 0);
                        // Assign a Lalafell clan deterministically based on their current clan ID
                        Marshal.WriteByte(addrClan, (byte) ((currentClan % 2) + LALAFELL_CLAN_OFFSET));

                        // Map any race-specific gear appropriately to Lalafellin gear
                        MapRacialGear(a);

                        // Trigger a re-render
                        Marshal.WriteInt32(addrRenderToggle, 2);
                        await Task.Delay(100);
                        Marshal.WriteInt32(addrRenderToggle, 0);
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.LogError(ex.ToString());
                }
            });
        }

        private void MapRacialGear(Dalamud.Game.ClientState.Actors.Types.Actor a)
        {
            Marshal.WriteInt16(a.Address + OFFSET_CHEST, MapRacialEquipModelId(Marshal.ReadInt16(a.Address + OFFSET_CHEST)));
            Marshal.WriteInt16(a.Address + OFFSET_ARMS, MapRacialEquipModelId(Marshal.ReadInt16(a.Address + OFFSET_ARMS)));
            Marshal.WriteInt16(a.Address + OFFSET_LEGS, MapRacialEquipModelId(Marshal.ReadInt16(a.Address + OFFSET_LEGS)));
            Marshal.WriteInt16(a.Address + OFFSET_FEET, MapRacialEquipModelId(Marshal.ReadInt16(a.Address + OFFSET_FEET)));
        }

        private short MapRacialEquipModelId(short input)
        {
            switch (input)
            {
                // Male
                case 84: // Hyur
                case 86: // Elezen
                case 88: // Miqo
                case 90: // Roe
                case 597: // Hrothgar
                    return 92;
                // Female
                case 85: // Hyur
                case 87: // Elezen
                case 89: // Miqo
                case 91: // Roe
                case 581: // Viera
                    return 93;
            }

            return input;
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            this.pluginInterface.Framework.OnUpdateEvent -= RenderActors;
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
