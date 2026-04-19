using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace PokeBlack2.Foundation.Editor
{
    public static class ExtractionManifestValidator
    {
        private static readonly string[] RequiredExportRootDirectories =
        {
            "raw",
            "normalized",
            "manifests",
            "logs",
        };

        public static ExtractionManifest LoadAndValidate(string manifestPath)
        {
            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException($"Manifest file was not found at '{manifestPath}'.");
            }

            ExtractionManifest manifest = DeserializeManifest(File.ReadAllText(manifestPath));

            Validate(manifest);
            return manifest;
        }

        public static void Validate(ExtractionManifest manifest)
        {
            if (manifest.SchemaVersion != Gen5ImportProfile.SchemaVersion)
            {
                throw new InvalidDataException(
                    $"Unsupported schema version '{manifest.SchemaVersion}'. Expected '{Gen5ImportProfile.SchemaVersion}'.");
            }

            if (!string.Equals(manifest.Game, Gen5ImportProfile.GameId, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Unsupported game id '{manifest.Game}'.");
            }

            if (manifest.Rom == null)
            {
                throw new InvalidDataException("Manifest rom block is required.");
            }

            RequireNonEmpty(manifest.Rom.Filename, "rom.filename");
            RequireNonEmpty(manifest.Rom.Sha1, "rom.sha1");

            if (manifest.Rom.Size <= 0)
            {
                throw new InvalidDataException("Manifest rom.size must be greater than zero.");
            }

            RequireNonEmpty(manifest.ExportRoot, "exportRoot");
            RequireNonEmpty(manifest.GeneratedAt, "generatedAt");

            if (Path.IsPathRooted(manifest.ExportRoot))
            {
                throw new InvalidDataException("Manifest exportRoot must stay relative to the Unity project root.");
            }

            RejectParentTraversal(manifest.ExportRoot, "exportRoot");

            if (manifest.NormalizedOutputs == null)
            {
                throw new InvalidDataException("Manifest normalizedOutputs list is required.");
            }

            if (manifest.Hashes == null)
            {
                throw new InvalidDataException("Manifest hashes map is required.");
            }

            string normalizedExportRoot = NormalizeManifestPath(manifest.ExportRoot);
            HashSet<string> seenOutputPaths = new HashSet<string>(StringComparer.Ordinal);

            foreach (NormalizedOutputManifestEntry entry in manifest.NormalizedOutputs)
            {
                if (entry == null)
                {
                    throw new InvalidDataException("Manifest normalized output entries cannot be null.");
                }

                RequireNonEmpty(entry.Path, "normalizedOutputs[].path");
                RequireNonEmpty(entry.Hash, "normalizedOutputs[].hash");

                if (Path.IsPathRooted(entry.Path))
                {
                    throw new InvalidDataException("Normalized output paths must stay relative to the Unity project root.");
                }

                RejectParentTraversal(entry.Path, "normalizedOutputs[].path");

                string normalizedOutputPath = NormalizeManifestPath(entry.Path);
                if (!normalizedOutputPath.StartsWith(normalizedExportRoot + "/", StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        $"Normalized output '{entry.Path}' must stay under exportRoot '{manifest.ExportRoot}'.");
                }

                if (!seenOutputPaths.Add(entry.Path))
                {
                    throw new InvalidDataException($"Manifest contains duplicate normalized output path '{entry.Path}'.");
                }

                if (!manifest.Hashes.TryGetValue(entry.Path, out string hashValue) || !string.Equals(hashValue, entry.Hash, StringComparison.Ordinal))
                {
                    throw new InvalidDataException($"Manifest hashes map is missing the digest for '{entry.Path}'.");
                }
            }
        }

        public static void ValidateExportRootLayout(string rootPath)
        {
            if (!Directory.Exists(rootPath))
            {
                throw new DirectoryNotFoundException($"Export root directory was not found at '{rootPath}'.");
            }

            HashSet<string> actualDirectories = Directory
                .GetDirectories(rootPath)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToHashSet(StringComparer.Ordinal);

            foreach (string requiredDirectory in RequiredExportRootDirectories)
            {
                if (!actualDirectories.Contains(requiredDirectory))
                {
                    throw new InvalidDataException($"Export root is missing the required '{requiredDirectory}' directory.");
                }
            }

            string unexpectedDirectory = actualDirectories
                .FirstOrDefault(name => !RequiredExportRootDirectories.Contains(name, StringComparer.Ordinal));

            if (!string.IsNullOrEmpty(unexpectedDirectory))
            {
                throw new InvalidDataException(
                    $"Unexpected export-root directory '{unexpectedDirectory}'. Only raw, normalized, manifests, and logs are allowed.");
            }
        }

        private static void RequireNonEmpty(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidDataException($"Manifest field '{fieldName}' must be a non-empty string.");
            }
        }

        private static ExtractionManifest DeserializeManifest(string json)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(
                typeof(ExtractionManifest),
                new DataContractJsonSerializerSettings
                {
                    UseSimpleDictionaryFormat = true,
                });
            using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            ExtractionManifest manifest = serializer.ReadObject(stream) as ExtractionManifest;
            return manifest ?? throw new InvalidDataException("Manifest JSON could not be deserialized.");
        }

        private static string NormalizeManifestPath(string path)
        {
            string normalized = path.Replace('\\', '/').Trim();
            while (normalized.StartsWith("./", StringComparison.Ordinal))
            {
                normalized = normalized.Substring(2);
            }

            return normalized.TrimEnd('/');
        }

        private static void RejectParentTraversal(string path, string fieldName)
        {
            string normalized = path.Replace('\\', '/');
            string[] segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
            foreach (string segment in segments)
            {
                if (string.Equals(segment, "..", StringComparison.Ordinal))
                {
                    throw new InvalidDataException($"Manifest field '{fieldName}' cannot traverse parent directories.");
                }
            }
        }
    }
}
