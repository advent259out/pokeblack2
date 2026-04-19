using System;

namespace PokeBlack2.Foundation.Runtime.Gen5.Contracts
{
    [Serializable]
    public sealed class TextTokenContract
    {
        public int[] Arguments = Array.Empty<int>();
        public int CodePoint = -1;
        public int ControlCode = -1;
        public string Kind = string.Empty;
        public string Text = string.Empty;
    }
}
