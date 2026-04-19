using System;
using System.Collections.Generic;
using PokeBlack.Content.Contracts;
using UnityEngine;

namespace PokeBlack.Content.Runtime
{
    [CreateAssetMenu(menuName = "PokeBlack/Content Manifest", fileName = "ContentManifest")]
    public sealed class ContentManifest : ScriptableObject
    {
        [SerializeField] private int schemaVersion = ContentSchemaVersions.ContentManifest;
        [SerializeField] private ContentVersionInfo version = new ContentVersionInfo();
        [SerializeField] private string gameId = string.Empty;
        [SerializeField] private string contractFamily = string.Empty;
        [SerializeField] private string profileId = string.Empty;
        [SerializeField] private string romFilename = string.Empty;
        [SerializeField] private string romSha1 = string.Empty;
        [SerializeField] private long romSize;
        [SerializeField] private string sourceGeneratedAt = string.Empty;
        [SerializeField] private string[] availableGroups = Array.Empty<string>();

        public int SchemaVersion => schemaVersion;
        public ContentVersionInfo Version => version;
        public int SourceSchemaVersion => version?.SourceSchemaVersion ?? 0;
        public string ContentVersion => version?.ContentVersion ?? string.Empty;
        public string GameId => gameId;
        public string ContractFamily => contractFamily;
        public string ProfileId => profileId;
        public string RomFilename => romFilename;
        public string RomSha1 => romSha1;
        public long RomSize => romSize;
        public string SourceGeneratedAt => sourceGeneratedAt;
        public IReadOnlyList<string> AvailableGroups => availableGroups ?? Array.Empty<string>();

        public void Configure(ContentManifestData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            schemaVersion = data.SchemaVersion;
            version = data.Version ?? new ContentVersionInfo();
            gameId = data.GameId ?? string.Empty;
            contractFamily = data.ContractFamily ?? string.Empty;
            profileId = data.ProfileId ?? string.Empty;
            romFilename = data.RomFilename ?? string.Empty;
            romSha1 = data.RomSha1 ?? string.Empty;
            romSize = data.RomSize;
            sourceGeneratedAt = data.SourceGeneratedAt ?? string.Empty;
            availableGroups = data.AvailableGroups ?? Array.Empty<string>();
        }

        public bool ContainsGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName) || availableGroups == null)
            {
                return false;
            }

            foreach (string availableGroup in availableGroups)
            {
                if (string.Equals(availableGroup, groupName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public void EnsureValid()
        {
            if (schemaVersion != ContentSchemaVersions.ContentManifest)
            {
                throw new InvalidOperationException(
                    $"ContentManifest schema version '{schemaVersion}' is unsupported. Expected '{ContentSchemaVersions.ContentManifest}'.");
            }

            if (version == null)
            {
                throw new InvalidOperationException("ContentManifest requires a content version block.");
            }

            if (version.SourceSchemaVersion <= 0)
            {
                throw new InvalidOperationException("ContentManifest requires a positive source schema version.");
            }

            RequireNonEmpty(version.ContentVersion, "content version");
            RequireNonEmpty(gameId, "game id");
            RequireNonEmpty(contractFamily, "contract family");
            RequireNonEmpty(profileId, "profile id");
            RequireNonEmpty(romFilename, "rom filename");
            RequireNonEmpty(romSha1, "rom sha1");
            RequireNonEmpty(sourceGeneratedAt, "source generatedAt");

            if (romSize <= 0)
            {
                throw new InvalidOperationException("ContentManifest requires a positive ROM size.");
            }

            if (availableGroups == null)
            {
                throw new InvalidOperationException("ContentManifest requires an available groups list.");
            }

            HashSet<string> seenGroups = new HashSet<string>(StringComparer.Ordinal);
            foreach (string groupName in availableGroups)
            {
                RequireNonEmpty(groupName, "available group");
                if (!seenGroups.Add(groupName))
                {
                    throw new InvalidOperationException($"ContentManifest contains duplicate available group '{groupName}'.");
                }
            }
        }

        private static void RequireNonEmpty(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"ContentManifest requires a non-empty {fieldName}.");
            }
        }
    }
}
