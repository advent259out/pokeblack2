using System;

namespace PokeBlack2.Foundation.Runtime.Gen5.Contracts
{
    [Serializable]
    public sealed class SaveProfileContract
    {
        public string ProfileId = "placeholder-save";
        public int SlotCount = 1;
        public string[] ReservedFlags = Array.Empty<string>();
    }
}

