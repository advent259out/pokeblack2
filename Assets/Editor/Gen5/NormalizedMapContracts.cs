using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PokeBlack2.Foundation.Editor
{
    [Serializable]
    [DataContract]
    public sealed class NormalizedMapGroupIndex
    {
        [DataMember(Name = "containerCount")]
        public int ContainerCount { get; set; }

        [DataMember(Name = "containers")]
        public List<NormalizedMapContainer> Containers { get; set; } = new List<NormalizedMapContainer>();

        [DataMember(Name = "group")]
        public string Group { get; set; } = string.Empty;

        [DataMember(Name = "totalScriptTextBindings")]
        public int TotalScriptTextBindings { get; set; }
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedMapContainer
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

        [DataMember(Name = "scriptTextBindingCount")]
        public int ScriptTextBindingCount { get; set; }

        [DataMember(Name = "scriptTextBindings")]
        public List<NormalizedMapScriptTextBinding> ScriptTextBindings { get; set; } = new List<NormalizedMapScriptTextBinding>();

        [DataMember(Name = "lookupCount")]
        public int LookupCount { get; set; }

        [DataMember(Name = "canonicalMapCount")]
        public int CanonicalMapCount { get; set; }

        [DataMember(Name = "identityMappingCount")]
        public int IdentityMappingCount { get; set; }

        [DataMember(Name = "aliasMappingCount")]
        public int AliasMappingCount { get; set; }

        [DataMember(Name = "maxResolvedMapIndex")]
        public int MaxResolvedMapIndex { get; set; } = -1;

        [DataMember(Name = "mapLookupEntries")]
        public List<NormalizedMapLookupEntry> MapLookupEntries { get; set; } = new List<NormalizedMapLookupEntry>();

        [DataMember(Name = "mapContainerLayoutCount")]
        public int MapContainerLayoutCount { get; set; }

        [DataMember(Name = "permissionGridCandidateMapCount")]
        public int PermissionGridCandidateMapCount { get; set; }

        [DataMember(Name = "permissionGridCandidateCount")]
        public int PermissionGridCandidateCount { get; set; }

        [DataMember(Name = "mapContainerLayouts")]
        public List<NormalizedMapContainerLayout> MapContainerLayouts { get; set; } = new List<NormalizedMapContainerLayout>();

        [DataMember(Name = "candidateCount")]
        public int CandidateCount { get; set; }

        [DataMember(Name = "seasonSlotCount")]
        public int SeasonSlotCount { get; set; }

        [DataMember(Name = "seasonWordCount")]
        public int SeasonWordCount { get; set; }

        [DataMember(Name = "mapMetadataCandidates")]
        public List<NormalizedMapMetadataCandidate> MapMetadataCandidates { get; set; } = new List<NormalizedMapMetadataCandidate>();

        [DataMember(Name = "sideLookupEntryCount")]
        public int SideLookupEntryCount { get; set; }

        [DataMember(Name = "distinctSideLookupPairCount")]
        public int DistinctSideLookupPairCount { get; set; }

        [DataMember(Name = "mapSideLookupEntries")]
        public List<NormalizedMapSideLookupEntry> MapSideLookupEntries { get; set; } = new List<NormalizedMapSideLookupEntry>();

        [DataMember(Name = "sha1")]
        public string Sha1 { get; set; } = string.Empty;

        [DataMember(Name = "size")]
        public int Size { get; set; }

        [DataMember(Name = "sourcePath")]
        public string SourcePath { get; set; } = string.Empty;

        [DataMember(Name = "totalMemberBytes")]
        public int TotalMemberBytes { get; set; }

        [DataMember(Name = "zoneCount")]
        public int ZoneCount { get; set; }

        [DataMember(Name = "zones")]
        public List<NormalizedZoneHeader> Zones { get; set; } = new List<NormalizedZoneHeader>();
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedZoneHeader
    {
        [DataMember(Name = "eventTextArchiveId")]
        public string EventTextArchiveId { get; set; } = string.Empty;

        [DataMember(Name = "eventTextBankIndex")]
        public int EventTextBankIndex { get; set; }

        [DataMember(Name = "primaryScriptMemberIndex")]
        public int PrimaryScriptMemberIndex { get; set; }

        [DataMember(Name = "scriptTextBindingCount")]
        public int ScriptTextBindingCount { get; set; }

        [DataMember(Name = "scriptTextBindings")]
        public List<NormalizedMapScriptTextBinding> ScriptTextBindings { get; set; } = new List<NormalizedMapScriptTextBinding>();

        [DataMember(Name = "secondaryScriptMemberIndex")]
        public int SecondaryScriptMemberIndex { get; set; }

        [DataMember(Name = "zoneIndex")]
        public int ZoneIndex { get; set; }
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedMapScriptTextBinding
    {
        [DataMember(Name = "scriptMemberIndex")]
        public int ScriptMemberIndex { get; set; }

        [DataMember(Name = "textArchiveId")]
        public string TextArchiveId { get; set; } = string.Empty;

        [DataMember(Name = "textBankIndex")]
        public int TextBankIndex { get; set; }

        [DataMember(Name = "zoneIndex")]
        public int ZoneIndex { get; set; }
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedMapLookupEntry
    {
        [DataMember(Name = "logicalMapIndex")]
        public int LogicalMapIndex { get; set; }

        [DataMember(Name = "resolvedMapIndex")]
        public int ResolvedMapIndex { get; set; }

        [DataMember(Name = "isIdentityMapping")]
        public bool IsIdentityMapping { get; set; }
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedMapContainerLayout
    {
        [DataMember(Name = "mapContainerIndex")]
        public int MapContainerIndex { get; set; }

        [DataMember(Name = "containerTag")]
        public string ContainerTag { get; set; } = string.Empty;

        [DataMember(Name = "sectionCount")]
        public int SectionCount { get; set; }

        [DataMember(Name = "modelSectionCount")]
        public int ModelSectionCount { get; set; }

        [DataMember(Name = "permissionGridCandidateCount")]
        public int PermissionGridCandidateCount { get; set; }

        [DataMember(Name = "sections")]
        public List<NormalizedMapContainerSection> Sections { get; set; } = new List<NormalizedMapContainerSection>();

        [DataMember(Name = "permissionGridCandidates")]
        public List<NormalizedPermissionGridCandidate> PermissionGridCandidates { get; set; } = new List<NormalizedPermissionGridCandidate>();
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedMapContainerSection
    {
        [DataMember(Name = "sectionIndex")]
        public int SectionIndex { get; set; }

        [DataMember(Name = "offset")]
        public int Offset { get; set; }

        [DataMember(Name = "size")]
        public int Size { get; set; }

        [DataMember(Name = "startsWithModelMagic")]
        public bool StartsWithModelMagic { get; set; }

        [DataMember(Name = "isPermissionGridCandidate")]
        public bool IsPermissionGridCandidate { get; set; }
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedPermissionGridCandidate
    {
        [DataMember(Name = "sectionIndex")]
        public int SectionIndex { get; set; }

        [DataMember(Name = "width")]
        public int Width { get; set; }

        [DataMember(Name = "height")]
        public int Height { get; set; }

        [DataMember(Name = "primaryCellCount")]
        public int PrimaryCellCount { get; set; }

        [DataMember(Name = "recordStrideBytes")]
        public int RecordStrideBytes { get; set; }

        [DataMember(Name = "recordCount")]
        public int RecordCount { get; set; }

        [DataMember(Name = "planeCount")]
        public int PlaneCount { get; set; }

        [DataMember(Name = "trailingRecordCount")]
        public int TrailingRecordCount { get; set; }

        [DataMember(Name = "recordTokenCount")]
        public int RecordTokenCount { get; set; }

        [DataMember(Name = "recordTokens")]
        public List<string> RecordTokens { get; set; } = new List<string>();
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedMapSideLookupEntry
    {
        [DataMember(Name = "entryIndex")]
        public int EntryIndex { get; set; }

        [DataMember(Name = "word0")]
        public int Word0 { get; set; }

        [DataMember(Name = "word1")]
        public int Word1 { get; set; }
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedMapMetadataCandidate
    {
        [DataMember(Name = "logicalMapIndex")]
        public int LogicalMapIndex { get; set; }

        [DataMember(Name = "seasonSlotCount")]
        public int SeasonSlotCount { get; set; }

        [DataMember(Name = "distinctSeasonProfileCount")]
        public int DistinctSeasonProfileCount { get; set; }

        [DataMember(Name = "trailingValue")]
        public int TrailingValue { get; set; }

        [DataMember(Name = "seasonProfiles")]
        public List<NormalizedSeasonSlotProfile> SeasonProfiles { get; set; } = new List<NormalizedSeasonSlotProfile>();
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedSeasonSlotProfile
    {
        [DataMember(Name = "seasonSlotIndex")]
        public int SeasonSlotIndex { get; set; }

        [DataMember(Name = "wordCount")]
        public int WordCount { get; set; }

        [DataMember(Name = "wordValues")]
        public List<int> WordValues { get; set; } = new List<int>();

        [DataMember(Name = "nonZeroWordCount")]
        public int NonZeroWordCount { get; set; }

        [DataMember(Name = "nonZeroWords")]
        public List<NormalizedSeasonSlotWordValue> NonZeroWords { get; set; } = new List<NormalizedSeasonSlotWordValue>();
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedSeasonSlotWordValue
    {
        [DataMember(Name = "wordIndex")]
        public int WordIndex { get; set; }

        [DataMember(Name = "value")]
        public int Value { get; set; }
    }
}
