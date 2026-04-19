using System;

namespace PokeBlack2.Foundation.Runtime.Gen5.Contracts
{
    [Serializable]
    public sealed class WorldMapRouteContract
    {
        public int LogicalMapIndex = -1;
        public int ResolvedMapIndex = -1;
        public bool IsIdentityMapping = true;
        public string[] CandidateSceneIds = Array.Empty<string>();
        public int[] CandidateZoneIndices = Array.Empty<int>();
        public WorldMapSideLookupContract SideLookup = new WorldMapSideLookupContract();
        public SeasonProfileContract[] SeasonalVariants = Array.Empty<SeasonProfileContract>();
    }
}
