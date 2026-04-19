using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PokeBlack2.Foundation.Editor
{
    [Serializable]
    [DataContract]
    public sealed class ExtractionManifest
    {
        [DataMember(Name = "schemaVersion")]
        public int SchemaVersion { get; set; }

        [DataMember(Name = "game")]
        public string Game { get; set; } = string.Empty;

        [DataMember(Name = "rom")]
        public ExtractionManifestRom Rom { get; set; } = new ExtractionManifestRom();

        [DataMember(Name = "exportRoot")]
        public string ExportRoot { get; set; } = string.Empty;

        [DataMember(Name = "generatedAt")]
        public string GeneratedAt { get; set; } = string.Empty;

        [DataMember(Name = "normalizedOutputs")]
        public List<NormalizedOutputManifestEntry> NormalizedOutputs { get; set; } = new List<NormalizedOutputManifestEntry>();

        [DataMember(Name = "hashes")]
        public Dictionary<string, string> Hashes { get; set; } = new Dictionary<string, string>();
    }

    [Serializable]
    [DataContract]
    public sealed class ExtractionManifestRom
    {
        [DataMember(Name = "filename")]
        public string Filename { get; set; } = string.Empty;

        [DataMember(Name = "sha1")]
        public string Sha1 { get; set; } = string.Empty;

        [DataMember(Name = "size")]
        public long Size { get; set; }
    }

    [Serializable]
    [DataContract]
    public sealed class NormalizedOutputManifestEntry
    {
        [DataMember(Name = "path")]
        public string Path { get; set; } = string.Empty;

        [DataMember(Name = "hash")]
        public string Hash { get; set; } = string.Empty;
    }
}
