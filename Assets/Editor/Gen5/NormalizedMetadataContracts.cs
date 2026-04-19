using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PokeBlack2.Foundation.Editor
{
    [Serializable]
    [DataContract]
    public sealed class NormalizedRomDescriptor
    {
        [DataMember(Name = "filename")]
        public string Filename { get; set; } = string.Empty;

        [DataMember(Name = "game")]
        public string Game { get; set; } = string.Empty;

        [DataMember(Name = "sha1")]
        public string Sha1 { get; set; } = string.Empty;

        [DataMember(Name = "size")]
        public long Size { get; set; }
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedRomInfo
    {
        [DataMember(Name = "fileCount")]
        public int FileCount { get; set; }

        [DataMember(Name = "filename")]
        public string Filename { get; set; } = string.Empty;

        [DataMember(Name = "game")]
        public string Game { get; set; } = string.Empty;

        [DataMember(Name = "namedFileCount")]
        public int NamedFileCount { get; set; }

        [DataMember(Name = "sha1")]
        public string Sha1 { get; set; } = string.Empty;

        [DataMember(Name = "size")]
        public long Size { get; set; }

        [DataMember(Name = "unnamedFileCount")]
        public int UnnamedFileCount { get; set; }
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedSourceCatalog
    {
        [DataMember(Name = "rom")]
        public NormalizedRomDescriptor Rom { get; set; } = new NormalizedRomDescriptor();

        [DataMember(Name = "sourceCount")]
        public int SourceCount { get; set; }

        [DataMember(Name = "sources")]
        public List<NormalizedSourceCatalogEntry> Sources { get; set; } = new List<NormalizedSourceCatalogEntry>();
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedSourceCatalogEntry
    {
        [DataMember(Name = "fileId")]
        public int FileId { get; set; }

        [DataMember(Name = "group")]
        public string Group { get; set; } = string.Empty;

        [DataMember(Name = "id")]
        public string Id { get; set; } = string.Empty;

        [DataMember(Name = "largestMemberSize")]
        public int LargestMemberSize { get; set; }

        [DataMember(Name = "memberCount")]
        public int MemberCount { get; set; }

        [DataMember(Name = "sha1")]
        public string Sha1 { get; set; } = string.Empty;

        [DataMember(Name = "size")]
        public int Size { get; set; }

        [DataMember(Name = "sourcePath")]
        public string SourcePath { get; set; } = string.Empty;
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedGroupIndex
    {
        [DataMember(Name = "containerCount")]
        public int ContainerCount { get; set; }

        [DataMember(Name = "containers")]
        public List<NormalizedGroupContainer> Containers { get; set; } = new List<NormalizedGroupContainer>();

        [DataMember(Name = "group")]
        public string Group { get; set; } = string.Empty;
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedGroupContainer
    {
        [DataMember(Name = "containerType")]
        public string ContainerType { get; set; } = string.Empty;

        [DataMember(Name = "fileId")]
        public int FileId { get; set; }

        [DataMember(Name = "id")]
        public string Id { get; set; } = string.Empty;

        [DataMember(Name = "largestMemberSize")]
        public int LargestMemberSize { get; set; }

        [DataMember(Name = "memberCount")]
        public int MemberCount { get; set; }

        [DataMember(Name = "members")]
        public List<NormalizedGroupMember> Members { get; set; } = new List<NormalizedGroupMember>();

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
    public sealed class NormalizedGroupMember
    {
        [DataMember(Name = "index")]
        public int Index { get; set; }

        [DataMember(Name = "sha1")]
        public string Sha1 { get; set; } = string.Empty;

        [DataMember(Name = "size")]
        public int Size { get; set; }
    }
}
