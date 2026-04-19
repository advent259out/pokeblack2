using System;

namespace PokeBlack.Content.Contracts
{
    [Serializable]
    public sealed class ContentManifestData
    {
        public int SchemaVersion { get; set; } = ContentSchemaVersions.ContentManifest;

        public ContentVersionInfo Version { get; set; } = new ContentVersionInfo();

        public string GameId { get; set; } = string.Empty;

        public string ContractFamily { get; set; } = string.Empty;

        public string ProfileId { get; set; } = string.Empty;

        public string RomFilename { get; set; } = string.Empty;

        public string RomSha1 { get; set; } = string.Empty;

        public long RomSize { get; set; }

        public string SourceGeneratedAt { get; set; } = string.Empty;

        public string[] AvailableGroups { get; set; } = Array.Empty<string>();
    }
}
