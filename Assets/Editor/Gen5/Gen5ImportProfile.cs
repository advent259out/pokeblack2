using System;
using System.Collections.Generic;

namespace PokeBlack2.Foundation.Editor
{
    public static class Gen5ImportProfile
    {
        private static readonly IReadOnlyList<string> SupportedNormalizedGroups = Array.AsReadOnly(
            new[]
        {
            "text",
            "maps",
            "scripts",
            "trainers",
            "pokemon",
            "encounters",
            "visual",
        });

        public const int SchemaVersion = 1;
        public const string GameId = "pokemon-black";
        public const string CanonicalExportRoot = "External/Exports/BlackWhite/M0";
        public const string CanonicalManifestRelativePath = "manifests/manifest.json";
        public const string NormalizedRootRelativePath = "normalized";
        public const string LogsRootRelativePath = "logs";
        public const string FoundationLogsRelativePath = "logs/foundation";
        public const string GeneratedAssetsRoot = "Assets/Generated";
        public const string GeneratedResourcesRoot = "Assets/Generated/Resources";
        public const string CanonicalProfileAssetPath = "Assets/Generated/Resources/Foundation/GameContentProfile.asset";
        public const string CanonicalScriptDatabaseAssetPath = "Assets/Generated/Resources/Imported/Gen5/Scripts/CanonicalGen5ScriptDatabase.asset";
        public const string CanonicalTextDatabaseAssetPath = "Assets/Generated/Resources/Imported/Gen5/Text/CanonicalGen5TextDatabase.asset";
        public const string CanonicalWorldDatabaseAssetPath = "Assets/Generated/Resources/Imported/Gen5/World/CanonicalGen5WorldDatabase.asset";
        public const string ContractPlaceholderRelativePath = "normalized/metadata/contract-placeholder.json";
        public const string RomInfoRelativePath = "normalized/metadata/rom-info.json";
        public const string SourceCatalogRelativePath = "normalized/metadata/source-catalog.json";
        public const string ValidationReportRelativePath = "logs/foundation/validation-report.json";
        public const string ValidationSummaryRelativePath = "logs/foundation/validation-summary.txt";

        public static IReadOnlyList<string> GetSupportedNormalizedGroups()
        {
            return SupportedNormalizedGroups;
        }

        public static string GetGroupIndexRelativePath(string groupName)
        {
            foreach (string supportedGroup in SupportedNormalizedGroups)
            {
                if (string.Equals(supportedGroup, groupName, System.StringComparison.Ordinal))
                {
                    return $"normalized/{groupName}/index.json";
                }
            }

            throw new System.ArgumentException($"Unsupported normalized group '{groupName}'.", nameof(groupName));
        }
    }
}
