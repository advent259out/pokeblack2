using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using PokeBlack.Content.Contracts;
using PokeBlack.Content.Runtime;
using PokeBlack2.Foundation.Runtime.Core;
using UnityEditor;
using UnityEngine;

namespace PokeBlack2.Foundation.Editor
{
    public static class ContentManifestImportUtility
    {
        public static ContentManifest ImportForSession(Gen5FoundationImportSession session, string generatedAssetsRoot)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (string.IsNullOrWhiteSpace(generatedAssetsRoot))
            {
                throw new ArgumentException("Generated assets root cannot be null or whitespace.", nameof(generatedAssetsRoot));
            }

            string normalizedGeneratedAssetsRoot = NormalizeAssetPath(generatedAssetsRoot);
            string contentManifestAssetPath = NormalizeAssetPath(
                Gen5ImportProfile.CanonicalContentManifestAssetPath.Replace(
                    Gen5ImportProfile.GeneratedAssetsRoot,
                    normalizedGeneratedAssetsRoot,
                    StringComparison.Ordinal));

            EnsureAssetFolder(Path.GetDirectoryName(contentManifestAssetPath)?.Replace('\\', '/'));

            ContentManifest contentManifest = AssetDatabase.LoadAssetAtPath<ContentManifest>(contentManifestAssetPath);
            if (contentManifest != null)
            {
                ValidateExistingManifest(contentManifest, contentManifestAssetPath);
            }
            else
            {
                contentManifest = ScriptableObject.CreateInstance<ContentManifest>();
                AssetDatabase.CreateAsset(contentManifest, contentManifestAssetPath);
            }

            contentManifest.name = "ContentManifest";
            contentManifest.Configure(CreateData(session));
            contentManifest.EnsureValid();
            EditorUtility.SetDirty(contentManifest);
            return contentManifest;
        }

        private static ContentManifestData CreateData(Gen5FoundationImportSession session)
        {
            List<string> availableGroups = new List<string>(session.AvailableGroups);
            availableGroups.Sort(StringComparer.Ordinal);

            return new ContentManifestData
            {
                SchemaVersion = ContentSchemaVersions.ContentManifest,
                Version = new ContentVersionInfo
                {
                    SourceSchemaVersion = session.Manifest.SchemaVersion,
                    ContentVersion = ComputeContentVersion(session.Manifest),
                },
                GameId = session.Manifest.Game,
                ContractFamily = GameContentProfile.DefaultContractFamily,
                ProfileId = GameContentProfile.DefaultProfileId,
                RomFilename = session.RomInfo.Filename,
                RomSha1 = session.RomInfo.Sha1,
                RomSize = session.RomInfo.Size,
                SourceGeneratedAt = session.Manifest.GeneratedAt,
                AvailableGroups = availableGroups.ToArray(),
            };
        }

        private static void ValidateExistingManifest(ContentManifest contentManifest, string assetPath)
        {
            if (contentManifest.SchemaVersion != ContentSchemaVersions.ContentManifest)
            {
                throw new InvalidDataException(
                    $"Existing ContentManifest asset at '{assetPath}' uses unsupported schema version '{contentManifest.SchemaVersion}'.");
            }

            contentManifest.EnsureValid();
        }

        internal static string ComputeContentVersion(ExtractionManifest manifest)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("content-version-v1");
            builder.AppendLine(manifest.Game ?? string.Empty);
            builder.AppendLine(manifest.Rom?.Filename ?? string.Empty);
            builder.AppendLine(manifest.Rom?.Sha1 ?? string.Empty);
            builder.AppendLine((manifest.Rom?.Size ?? 0L).ToString());
            builder.AppendLine(manifest.SchemaVersion.ToString());

            List<NormalizedOutputManifestEntry> normalizedOutputs =
                manifest.NormalizedOutputs == null
                    ? new List<NormalizedOutputManifestEntry>()
                    : new List<NormalizedOutputManifestEntry>(manifest.NormalizedOutputs);
            normalizedOutputs.Sort((left, right) => string.Compare(left?.Path, right?.Path, StringComparison.Ordinal));

            foreach (NormalizedOutputManifestEntry entry in normalizedOutputs)
            {
                builder.Append(entry?.Path ?? string.Empty);
                builder.Append('=');
                builder.Append(entry?.Hash ?? string.Empty);
                builder.AppendLine();
            }

            using SHA1 sha1 = SHA1.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(builder.ToString());
            byte[] hash = sha1.ComputeHash(bytes);
            StringBuilder versionBuilder = new StringBuilder(hash.Length * 2);
            foreach (byte value in hash)
            {
                versionBuilder.Append(value.ToString("x2"));
            }

            return versionBuilder.ToString();
        }

        private static void EnsureAssetFolder(string assetFolderPath)
        {
            if (string.IsNullOrWhiteSpace(assetFolderPath))
            {
                throw new ArgumentException("Asset folder path cannot be null or whitespace.", nameof(assetFolderPath));
            }

            string normalizedPath = NormalizeAssetPath(assetFolderPath);
            if (!normalizedPath.StartsWith("Assets", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Asset folder '{assetFolderPath}' must stay under the Unity Assets root.");
            }

            string[] segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            string currentPath = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string nextPath = $"{currentPath}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, segments[index]);
                }

                currentPath = nextPath;
            }
        }

        private static string NormalizeAssetPath(string assetPath)
        {
            return assetPath.Replace('\\', '/').TrimEnd('/');
        }
    }
}
