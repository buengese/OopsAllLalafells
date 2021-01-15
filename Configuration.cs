using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace OopsAllLalafells
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; }
        public byte TargetRace { get; set; } = 3;
        [JsonIgnore] private DalamudPluginInterface pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface.SavePluginConfig(this);
        }
    }
}