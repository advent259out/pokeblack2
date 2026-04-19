using System;

namespace PokeBlack2.Foundation.Runtime.Gen5.Contracts
{
    [Serializable]
    public sealed class WorldMapReferenceContract
    {
        public int LogicalMapIndex = -1;
        public int ResolvedMapIndex = -1;
        public bool IsIdentityMapping = true;
    }
}
