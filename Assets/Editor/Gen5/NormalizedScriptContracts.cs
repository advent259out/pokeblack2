using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PokeBlack2.Foundation.Editor
{
    [Serializable]
    [DataContract]
    public sealed class NormalizedScriptGroupIndex
    {
        [DataMember(Name = "containerCount")]
        public int ContainerCount { get; set; }

        [DataMember(Name = "containers")]
        public List<NormalizedScriptContainer> Containers { get; set; } = new List<NormalizedScriptContainer>();

        [DataMember(Name = "group")]
        public string Group { get; set; } = string.Empty;

        [DataMember(Name = "totalDecodedDialogueLines")]
        public int TotalDecodedDialogueLines { get; set; }

        [DataMember(Name = "totalDecodedProcedures")]
        public int TotalDecodedProcedures { get; set; }

        [DataMember(Name = "totalParsedProcedures")]
        public int TotalParsedProcedures { get; set; }
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedScriptContainer
    {
        [DataMember(Name = "containerType")]
        public string ContainerType { get; set; } = string.Empty;

        [DataMember(Name = "decodedDialogueLineCount")]
        public int DecodedDialogueLineCount { get; set; }

        [DataMember(Name = "decodedProcedureCount")]
        public int DecodedProcedureCount { get; set; }

        [DataMember(Name = "fileId")]
        public int FileId { get; set; }

        [DataMember(Name = "id")]
        public string Id { get; set; } = string.Empty;

        [DataMember(Name = "largestMemberSize")]
        public int LargestMemberSize { get; set; }

        [DataMember(Name = "memberCount")]
        public int MemberCount { get; set; }

        [DataMember(Name = "members")]
        public List<NormalizedScriptFile> Members { get; set; } = new List<NormalizedScriptFile>();

        [DataMember(Name = "parsedProcedureCount")]
        public int ParsedProcedureCount { get; set; }

        [DataMember(Name = "rawOutputPath")]
        public string RawOutputPath { get; set; } = string.Empty;

        [DataMember(Name = "sha1")]
        public string Sha1 { get; set; } = string.Empty;

        [DataMember(Name = "size")]
        public int Size { get; set; }

        [DataMember(Name = "sourcePath")]
        public string SourcePath { get; set; } = string.Empty;

        [DataMember(Name = "totalMemberBytes")]
        public int TotalMemberBytes { get; set; }
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedScriptFile
    {
        [DataMember(Name = "dialogueLineCount")]
        public int DialogueLineCount { get; set; }

        [DataMember(Name = "dialogueLines")]
        public List<NormalizedScriptDialogueLine> DialogueLines { get; set; } = new List<NormalizedScriptDialogueLine>();

        [DataMember(Name = "headerEntries")]
        public List<NormalizedScriptHeaderEntry> HeaderEntries { get; set; } = new List<NormalizedScriptHeaderEntry>();

        [DataMember(Name = "headerEntryCount")]
        public int HeaderEntryCount { get; set; }

        [DataMember(Name = "headerMarkerOffset")]
        public int? HeaderMarkerOffset { get; set; }

        [DataMember(Name = "index")]
        public int Index { get; set; }

        [DataMember(Name = "parseWarningCount")]
        public int ParseWarningCount { get; set; }

        [DataMember(Name = "parseWarnings")]
        public List<string> ParseWarnings { get; set; } = new List<string>();

        [DataMember(Name = "parsedProcedureCount")]
        public int ParsedProcedureCount { get; set; }

        [DataMember(Name = "procedureCount")]
        public int ProcedureCount { get; set; }

        [DataMember(Name = "procedures")]
        public List<NormalizedScriptProcedure> Procedures { get; set; } = new List<NormalizedScriptProcedure>();

        [DataMember(Name = "sha1")]
        public string Sha1 { get; set; } = string.Empty;

        [DataMember(Name = "size")]
        public int Size { get; set; }
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedScriptHeaderEntry
    {
        [DataMember(Name = "headerIndex")]
        public int HeaderIndex { get; set; }

        [DataMember(Name = "headerOffset")]
        public int HeaderOffset { get; set; }

        [DataMember(Name = "startOffset")]
        public int StartOffset { get; set; }

        [DataMember(Name = "storedOffset")]
        public int StoredOffset { get; set; }
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedScriptProcedure
    {
        [DataMember(Name = "dialogueLineCount")]
        public int DialogueLineCount { get; set; }

        [DataMember(Name = "dialogueLines")]
        public List<NormalizedScriptDialogueLine> DialogueLines { get; set; } = new List<NormalizedScriptDialogueLine>();

        [DataMember(Name = "endOffset")]
        public int EndOffset { get; set; }

        [DataMember(Name = "entryKind")]
        public string EntryKind { get; set; } = string.Empty;

        [DataMember(Name = "headerIndex")]
        public int? HeaderIndex { get; set; }

        [DataMember(Name = "instructionCount")]
        public int InstructionCount { get; set; }

        [DataMember(Name = "instructions")]
        public List<NormalizedScriptInstruction> Instructions { get; set; } = new List<NormalizedScriptInstruction>();

        [DataMember(Name = "parseStatus")]
        public string ParseStatus { get; set; } = string.Empty;

        [DataMember(Name = "procedureId")]
        public string ProcedureId { get; set; } = string.Empty;

        [DataMember(Name = "startOffset")]
        public int StartOffset { get; set; }
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedScriptInstruction
    {
        [DataMember(Name = "branchTargetOffset")]
        public int? BranchTargetOffset { get; set; }

        [DataMember(Name = "byteLength")]
        public int ByteLength { get; set; }

        [DataMember(Name = "mnemonic")]
        public string Mnemonic { get; set; } = string.Empty;

        [DataMember(Name = "offset")]
        public int Offset { get; set; }

        [DataMember(Name = "opcode")]
        public int Opcode { get; set; }

        [DataMember(Name = "operands")]
        public List<int> Operands { get; set; } = new List<int>();
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedScriptDialogueLine
    {
        [DataMember(Name = "command")]
        public string Command { get; set; } = string.Empty;

        [DataMember(Name = "instructionOffset")]
        public int InstructionOffset { get; set; }

        [DataMember(Name = "lineId")]
        public string LineId { get; set; } = string.Empty;

        [DataMember(Name = "messageId")]
        public int MessageId { get; set; }

        [DataMember(Name = "messageType")]
        public int? MessageType { get; set; }

        [DataMember(Name = "procedureId")]
        public string ProcedureId { get; set; } = string.Empty;

        [DataMember(Name = "speakerObjectId")]
        public int? SpeakerObjectId { get; set; }

        [DataMember(Name = "variantA")]
        public int? VariantA { get; set; }

        [DataMember(Name = "variantB")]
        public int? VariantB { get; set; }

        [DataMember(Name = "viewType")]
        public int? ViewType { get; set; }
    }
}
