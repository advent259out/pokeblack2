using System;

namespace PokeBlack2.Foundation.Runtime.Gen5.Contracts
{
    [Serializable]
    public sealed class ScriptDialogueLineContract
    {
        public string LineId = string.Empty;
        public string ProcedureId = string.Empty;
        public int InstructionOffset = -1;
        public string Command = string.Empty;
        public int MessageId = -1;
        public int SpeakerObjectId = -1;
        public int ViewType = -1;
        public int MessageType = -1;
        public int VariantA = -1;
        public int VariantB = -1;
        public string SpeakerId = string.Empty;
        public ScriptTextReferenceContract Text = new ScriptTextReferenceContract();
    }
}
