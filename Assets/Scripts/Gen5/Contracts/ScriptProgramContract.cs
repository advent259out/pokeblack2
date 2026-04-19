using System;

namespace PokeBlack2.Foundation.Runtime.Gen5.Contracts
{
    [Serializable]
    public sealed class ScriptProgramContract
    {
        public string ProgramId = "placeholder-script";
        public string ArchiveId = string.Empty;
        public int MemberIndex = -1;
        public string MemberSha1 = string.Empty;
        public int MemberSize = 0;
        public int HeaderMarkerOffset = -1;
        public ScriptHeaderEntryContract[] HeaderEntries = Array.Empty<ScriptHeaderEntryContract>();
        public ScriptProcedureContract[] Procedures = Array.Empty<ScriptProcedureContract>();
        public string[] ParseWarnings = Array.Empty<string>();
        public string[] OperationTokens = Array.Empty<string>();
        public ScriptDialogueLineContract[] DialogueLines = Array.Empty<ScriptDialogueLineContract>();
    }
}
