using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PokeBlack2.Foundation.Editor
{
    [Serializable]
    [DataContract]
    public sealed class NormalizedTextGroupIndex
    {
        [DataMember(Name = "containerCount")]
        public int ContainerCount { get; set; }

        [DataMember(Name = "containers")]
        public List<NormalizedTextContainer> Containers { get; set; } = new List<NormalizedTextContainer>();

        [DataMember(Name = "group")]
        public string Group { get; set; } = string.Empty;

        [DataMember(Name = "totalDecodedMessages")]
        public int TotalDecodedMessages { get; set; }
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedTextContainer
    {
        [DataMember(Name = "containerType")]
        public string ContainerType { get; set; } = string.Empty;

        [DataMember(Name = "decodedMessageCount")]
        public int DecodedMessageCount { get; set; }

        [DataMember(Name = "fileId")]
        public int FileId { get; set; }

        [DataMember(Name = "id")]
        public string Id { get; set; } = string.Empty;

        [DataMember(Name = "largestMemberSize")]
        public int LargestMemberSize { get; set; }

        [DataMember(Name = "memberCount")]
        public int MemberCount { get; set; }

        [DataMember(Name = "members")]
        public List<NormalizedTextBank> Members { get; set; } = new List<NormalizedTextBank>();

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
    public sealed class NormalizedTextBank
    {
        [DataMember(Name = "blockCount")]
        public int BlockCount { get; set; }

        [DataMember(Name = "index")]
        public int Index { get; set; }

        [DataMember(Name = "messageCount")]
        public int MessageCount { get; set; }

        [DataMember(Name = "messages")]
        public List<NormalizedTextMessage> Messages { get; set; } = new List<NormalizedTextMessage>();

        [DataMember(Name = "messagesPerBlock")]
        public int MessagesPerBlock { get; set; }

        [DataMember(Name = "sha1")]
        public string Sha1 { get; set; } = string.Empty;

        [DataMember(Name = "size")]
        public int Size { get; set; }
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedTextMessage
    {
        [DataMember(Name = "blockIndex")]
        public int BlockIndex { get; set; }

        [DataMember(Name = "charCount")]
        public int CharCount { get; set; }

        [DataMember(Name = "entryIndex")]
        public int EntryIndex { get; set; }

        [DataMember(Name = "flags")]
        public int Flags { get; set; }

        [DataMember(Name = "isCompressed")]
        public bool IsCompressed { get; set; }

        [DataMember(Name = "text")]
        public string Text { get; set; } = string.Empty;

        [DataMember(Name = "tokens")]
        public List<NormalizedTextToken> Tokens { get; set; } = new List<NormalizedTextToken>();
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedTextToken
    {
        [DataMember(Name = "arguments")]
        public List<int> Arguments { get; set; } = new List<int>();

        [DataMember(Name = "codePoint")]
        public int? CodePoint { get; set; }

        [DataMember(Name = "controlCode")]
        public int? ControlCode { get; set; }

        [DataMember(Name = "kind")]
        public string Kind { get; set; } = string.Empty;

        [DataMember(Name = "text")]
        public string Text { get; set; } = string.Empty;
    }
}
