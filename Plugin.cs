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

        private const int LALAFELL_RACE_ID = 3;
        private const int LALAFELL_CLAN_OFFSET = 5;

        private const int OFFSET_RACE = 0x1878;
        private const int OFFSET_CLAN = 0x187C;
        private const int OFFSET_RENDER_TOGGLE = 0x104;
        private const int OFFSET_MODEL_TYPE = 0x1B4;

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
                    || actor.ActorId == localPlayer.ActorId)
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
