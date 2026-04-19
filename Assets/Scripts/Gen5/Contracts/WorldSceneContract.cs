using System;

namespace PokeBlack2.Foundation.Runtime.Gen5.Contracts
{
    [Serializable]
    public sealed class WorldSceneContract
    {
        public string SceneId = "placeholder-scene";
        public string SourceId = "normalized-scene-shell";
        public int ZoneIndex = -1;
        public int PrimaryScriptMemberIndex = -1;
        public int SecondaryScriptMemberIndex = -1;
        public string EventTextArchiveId = string.Empty;
        public int EventTextBankIndex = -1;
        public WorldMapReferenceContract MapReference = new WorldMapReferenceContract();
        public PermissionGridContract PermissionGrid = new PermissionGridContract();
        public CameraProfileContract CameraProfile = new CameraProfileContract();
        public SeasonProfileContract[] SeasonalVariants = Array.Empty<SeasonProfileContract>();
    }
}
