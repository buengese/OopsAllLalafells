using System;

using Dalamud.Game;
using Dalamud.Logging;

namespace OopsAllLalafells;

/// <summary>
/// Plugin address resolver.
/// </summary>
internal class PluginAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets the address of the member ComboTimer.
    /// </summary>
    public IntPtr CharacterIsMount { get; private set; }
    
    /// <summary>
    /// Gets the address of fpGetAdjustedActionId.
    /// </summary>
    public IntPtr CharacterInitialize { get; private set; }

    /// <summary>
    /// Gets the address of fpIsIconReplacable.
    /// </summary>
    public IntPtr FlagSlotUpdate { get; private set; }

    /// <inheritdoc/>
    protected override void Setup64Bit(SigScanner scanner)
    {
        this.CharacterIsMount = scanner.ScanText("40 53 48 83 EC 20 48 8B 01 48 8B D9 FF 50 10 83 F8 08 75 08");

        this.CharacterInitialize = scanner.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 48 8B F9 48 8B EA 48 81 C1 ?? ?? ?? ?? E8 ?? ?? ?? ??");

        this.FlagSlotUpdate = scanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B DA 49 8B F0 48 8B F9 83 FA 0A");

        PluginLog.Verbose("===== OopsAllLalafells2 =====");
        PluginLog.Verbose($"{nameof(this.CharacterIsMount)}    0x{this.CharacterIsMount:X}");
        PluginLog.Verbose($"{nameof(this.CharacterInitialize)} 0x{this.CharacterInitialize:X}");
        PluginLog.Verbose($"{nameof(this.FlagSlotUpdate)}      0x{this.FlagSlotUpdate:X}");
    }
}