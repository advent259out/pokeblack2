using System;

namespace PokeBlack2.Foundation.Runtime.Gen5.Contracts
{
    [Serializable]
    public sealed class TextEntryContract
    {
        public int BlockCount;
        public int Index;
        public int MessageCount;
        public int MessagesPerBlock;
        public TextMessageContract[] Messages = Array.Empty<TextMessageContract>();
        public string Sha1 = string.Empty;
        public int Size;
    }
}
