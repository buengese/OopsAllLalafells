using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors;

namespace OopsAllLalafells
{
    [StructLayout((LayoutKind.Explicit))]
    public struct CharaCustomizeData
    {
        [FieldOffset((int) CustomizeIndex.Race)] public byte race;
        [FieldOffset((int) CustomizeIndex.Gender)] public byte gender; // 0 = male, 1 = female
        [FieldOffset((int) CustomizeIndex.Tribe)] public byte clan;
        [FieldOffset((int) CustomizeIndex.FaceType)] public byte faceType;
        [FieldOffset((int) CustomizeIndex.ModelType)] public byte bodyType;
    }
}
