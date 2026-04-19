using System;

namespace PokeBlack2.Foundation.Runtime.Gen5.Contracts
{
    [Serializable]
    public sealed class TextMessageContract
    {
        public int BlockIndex;
        public int CharCount;
        public int EntryIndex;
        public int Flags;
        public bool IsCompressed;
        public string Text = string.Empty;
        public TextTokenContract[] Tokens = Array.Empty<TextTokenContract>();
    }
}
