using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors;

namespace OopsAllLalafells
{
    [StructLayout((LayoutKind.Explicit))]
    public struct CharaCustomizeData
    {
        [FieldOffset((int) CustomizeIndex.Race)] public byte race;
        [FieldOffset((int) CustomizeIndex.Gender)] public byte gender; // 0 = male, 1 = female
        [FieldOffset((int) CustomizeIndex.Tribe)] public byte tribe;
        [FieldOffset((int) CustomizeIndex.FaceType)] public byte faceType;
        [FieldOffset((int) CustomizeIndex.SkinColor)] public byte skinColor;
        [FieldOffset((int) CustomizeIndex.ModelType)] public byte modelType;
        [FieldOffset((int) CustomizeIndex.HairColor)] public byte hairColor;
        [FieldOffset((int) CustomizeIndex.HairColor2)] public byte hairColor2;
        [FieldOffset((int) CustomizeIndex.HairStyle)] public byte hairStyle;
        [FieldOffset((int) CustomizeIndex.BustSize)] public byte bustSize;
        [FieldOffset((int) CustomizeIndex.RaceFeatureSize)] public byte raceFeatureSize;
        [FieldOffset((int) CustomizeIndex.RaceFeatureType)] public byte raceFeatureType;
    }
}
