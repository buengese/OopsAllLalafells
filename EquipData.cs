using System.Runtime.InteropServices;

namespace OopsAllLalafells
{
    [StructLayout(LayoutKind.Explicit)]
    public struct EquipData
    {
        [FieldOffset(0x0)] public short model;
        [FieldOffset(0x2)] public byte variant;
        [FieldOffset(0x3)] public byte dye;
    }
}