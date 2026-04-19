using System;

namespace PokeBlack2.Foundation.Runtime.Gen5.Contracts
{
    [Serializable]
    public sealed class ScriptHeaderEntryContract
    {
        public int HeaderIndex = -1;
        public int HeaderOffset = -1;
        public int StoredOffset = -1;
        public int StartOffset = -1;
    }
}
