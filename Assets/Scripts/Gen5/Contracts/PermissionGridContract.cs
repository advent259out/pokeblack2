using System;

namespace PokeBlack2.Foundation.Runtime.Gen5.Contracts
{
    [Serializable]
    public sealed class PermissionGridContract
    {
        public string GridId = "placeholder-grid";
        public int Width = 1;
        public int Height = 1;
        public string[] CellTokens = Array.Empty<string>();
    }
}

