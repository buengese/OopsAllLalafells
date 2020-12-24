using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors;

namespace OopsAllLalafells
{
    [StructLayout((LayoutKind.Explicit))]
    public struct CharaCustomizeData
    {
        [FieldOffset((int) CustomizeIndex.Race)] public byte race;
        [FieldOffset((int) CustomizeIndex.Gender)] public Gender gender;
        [FieldOffset((int) CustomizeIndex.Tribe)] public byte clan;
        [FieldOffset((int) CustomizeIndex.FaceType)] public byte faceType;
        [FieldOffset((int) CustomizeIndex.ModelType)] public byte bodyType;
    }

    public enum Gender : byte
    {
        Male = 0x0,
        Female = 0x1
    }
}
