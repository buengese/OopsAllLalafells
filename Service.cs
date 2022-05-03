using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Buddy;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Network;
using Dalamud.IoC;
using Dalamud.Plugin;

namespace OopsAllLalafells;

internal class Service
{
    /// <summary>
    /// Gets or sets the plugin address resolver.
    /// </summary>
    internal static PluginAddressResolver Address { get; set; } = null!;

    /// <summary>
    /// Gets the Dalamud plugin interface.
    /// </summary>
    [PluginService]
    internal static DalamudPluginInterface Interface { get; private set; } = null!;

    /// <summary>
    /// Gets the Dalamud client state.
    /// </summary>
    [PluginService]
    internal static ClientState ClientState { get; private set; } = null!;

    /// <summary>
    /// Gets the Dalamud command manager.
    /// </summary>
    [PluginService]
    internal static CommandManager CommandManager { get; private set; } = null!;


    /// <summary>
    /// Gets the Dalamud object table.
    /// </summary>
    [PluginService]
    internal static ObjectTable ObjectTable { get; private set; } = null!;
}
