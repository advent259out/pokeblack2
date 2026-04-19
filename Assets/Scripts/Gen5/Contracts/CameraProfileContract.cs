using System;

namespace PokeBlack2.Foundation.Runtime.Gen5.Contracts
{
    [Serializable]
    public sealed class CameraProfileContract
    {
        public string ProfileId = "placeholder-camera";
        public string CameraMode = "gen5-orbit";
        public float DefaultDistance = 10f;
        public float DefaultPitch = 35f;
    }
}

