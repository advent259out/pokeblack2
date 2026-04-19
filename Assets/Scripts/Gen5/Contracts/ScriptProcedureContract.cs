using System;

namespace PokeBlack2.Foundation.Runtime.Gen5.Contracts
{
    [Serializable]
    public sealed class ScriptProcedureContract
    {
        public string ProcedureId = string.Empty;
        public string EntryKind = string.Empty;
        public int HeaderIndex = -1;
        public int StartOffset = -1;
        public int EndOffset = -1;
        public string ParseStatus = string.Empty;
        public ScriptInstructionContract[] Instructions = Array.Empty<ScriptInstructionContract>();
    }
}
