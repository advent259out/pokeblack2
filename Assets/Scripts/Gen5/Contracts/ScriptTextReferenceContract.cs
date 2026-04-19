using System;

namespace PokeBlack2.Foundation.Runtime.Gen5.Contracts
{
    [Serializable]
    public sealed class ScriptTextReferenceContract
    {
        public string ArchiveId = string.Empty;
        public int BankIndex = -1;
        public int MessageIndex = -1;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(ArchiveId) &&
                   BankIndex >= 0 &&
                   MessageIndex >= 0;
        }
    }
}
