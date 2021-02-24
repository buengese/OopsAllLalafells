using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors;

namespace OopsAllLalafells {
    [StructLayout((LayoutKind.Explicit))]
    public struct CharaCustomizeData {
        [FieldOffset((int) CustomizeIndex.Race)] public Race Race;
        [FieldOffset((int) CustomizeIndex.Gender)] public byte Gender;
        [FieldOffset((int) CustomizeIndex.ModelType)] public byte ModelType;
        [FieldOffset((int) CustomizeIndex.Tribe)] public byte Tribe;
        [FieldOffset((int) CustomizeIndex.FaceType)] public byte FaceType;
        [FieldOffset((int) CustomizeIndex.HairStyle)] public byte HairStyle;
        [FieldOffset((int) CustomizeIndex.LipColor)] public byte LipColor;
    }
}
