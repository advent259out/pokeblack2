using System;

namespace PokeBlack2.Foundation.Runtime.Gen5.Contracts
{
    [Serializable]
    public sealed class TextArchiveContract
    {
        public string ArchiveId = string.Empty;
        public string ContainerType = string.Empty;
        public int FileId;
        public string SourcePath = string.Empty;
        public string RawOutputPath = string.Empty;
        public string Sha1 = string.Empty;
        public int ContainerSize;
        public int LargestMemberSize;
        public int MemberCount;
        public int TotalMemberBytes;
        public TextEntryContract[] Entries = Array.Empty<TextEntryContract>();
    }
}
