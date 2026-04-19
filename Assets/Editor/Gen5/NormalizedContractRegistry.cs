using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;

namespace PokeBlack2.Foundation.Editor
{
    public sealed class NormalizedContractRegistry
    {
        private readonly Dictionary<string, string> hashes;
        private readonly Dictionary<string, string> outputPaths;

        private NormalizedContractRegistry(string rootPath, ExtractionManifest manifest)
        {
            RootPath = Path.GetFullPath(rootPath);
            Manifest = manifest;
            EnsureRootMatchesManifest();
            hashes = new Dictionary<string, string>(manifest.Hashes, StringComparer.Ordinal);
            outputPaths = BuildOutputPathIndex(manifest);
        }

        public string RootPath { get; }
        public ExtractionManifest Manifest { get; }

        public IReadOnlyDictionary<string, string> Hashes => hashes;

        public static NormalizedContractRegistry LoadFromRoot(string rootPath)
        {
            ExtractionManifestValidator.ValidateExportRootLayout(rootPath);
            string manifestPath = Path.Combine(rootPath, "manifests", "manifest.json");
            ExtractionManifest manifest = ExtractionManifestValidator.LoadAndValidate(manifestPath);
            return new NormalizedContractRegistry(rootPath, manifest);
        }

        public bool ContainsOutput(string relativePath)
        {
            return outputPaths.ContainsKey(NormalizeRelativePath(relativePath));
        }

        public string ResolveOutputPath(string relativePath)
        {
            string normalizedPath = NormalizeRelativePath(relativePath);
            if (!outputPaths.TryGetValue(normalizedPath, out string outputPath))
            {
                throw new FileNotFoundException($"Normalized output '{relativePath}' is not registered in the manifest.");
            }

            return outputPath;
        }

        public bool ContainsRootRelativeOutput(string relativePathWithinRoot)
        {
            return ContainsOutput(BuildManifestRelativePath(relativePathWithinRoot));
        }

        public NormalizedRomInfo LoadRomInfo()
        {
            return LoadKnownJson<NormalizedRomInfo>(Gen5ImportProfile.RomInfoRelativePath);
        }

        public NormalizedSourceCatalog LoadSourceCatalog()
        {
            return LoadKnownJson<NormalizedSourceCatalog>(Gen5ImportProfile.SourceCatalogRelativePath);
        }

        public NormalizedGroupIndex LoadGroupIndex(string groupName)
        {
            return LoadKnownJson<NormalizedGroupIndex>(Gen5ImportProfile.GetGroupIndexRelativePath(groupName));
        }

        public NormalizedTextGroupIndex LoadTextGroupIndex()
        {
            return LoadKnownJson<NormalizedTextGroupIndex>(Gen5ImportProfile.GetGroupIndexRelativePath("text"));
        }

        public NormalizedMapGroupIndex LoadMapGroupIndex()
        {
            return LoadKnownJson<NormalizedMapGroupIndex>(Gen5ImportProfile.GetGroupIndexRelativePath("maps"));
        }

        public NormalizedScriptGroupIndex LoadScriptGroupIndex()
        {
            return LoadKnownJson<NormalizedScriptGroupIndex>(Gen5ImportProfile.GetGroupIndexRelativePath("scripts"));
        }

        public string LoadText(string relativePath)
        {
            return File.ReadAllText(ResolveOutputPath(relativePath));
        }

        public T LoadJson<T>(string relativePath)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(
                typeof(T),
                new DataContractJsonSerializerSettings
                {
                    UseSimpleDictionaryFormat = true,
                });

            using FileStream stream = File.OpenRead(ResolveOutputPath(relativePath));
            object result = serializer.ReadObject(stream);
            if (result is T typedResult)
            {
                return typedResult;
            }

            throw new InvalidDataException(
                $"Normalized output '{relativePath}' could not be deserialized as '{typeof(T).FullName}'.");
        }

        private void EnsureRootMatchesManifest()
        {
            string manifestRoot = Path.GetFullPath(Manifest.ExportRoot);
            if (!PathsEqual(RootPath, manifestRoot))
            {
                throw new InvalidDataException(
                    $"Manifest exportRoot '{Manifest.ExportRoot}' does not match the requested root '{RootPath}'.");
            }
        }

        private Dictionary<string, string> BuildOutputPathIndex(ExtractionManifest manifest)
        {
            Dictionary<string, string> index = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (NormalizedOutputManifestEntry entry in manifest.NormalizedOutputs)
            {
                string normalizedManifestPath = NormalizeRelativePath(entry.Path);
                string fullPath = Path.GetFullPath(entry.Path);
                EnsurePathWithinRoot(fullPath, entry.Path);

                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException(
                        $"Normalized output '{entry.Path}' is missing on disk at '{fullPath}'.",
                        fullPath);
                }

                string actualHash = ComputeSha1(fullPath);
                if (!string.Equals(actualHash, entry.Hash, StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        $"Normalized output '{entry.Path}' hash mismatch. Expected '{entry.Hash}', got '{actualHash}'.");
                }

                index.Add(normalizedManifestPath, fullPath);
            }

            return index;
        }

        private T LoadKnownJson<T>(string relativePathWithinRoot)
        {
            return LoadJson<T>(BuildManifestRelativePath(relativePathWithinRoot));
        }

        private string BuildManifestRelativePath(string relativePathWithinRoot)
        {
            string normalizedRelativePath = NormalizeRelativePath(relativePathWithinRoot);
            string normalizedRoot = NormalizeRelativePath(Manifest.ExportRoot);
            return NormalizeRelativePath($"{normalizedRoot}/{normalizedRelativePath}");
        }

        private void EnsurePathWithinRoot(string fullPath, string manifestPath)
        {
            string normalizedRoot = AppendDirectorySeparator(RootPath);
            if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"Normalized output '{manifestPath}' resolves outside export root '{Manifest.ExportRoot}'.");
            }
        }

        private static bool PathsEqual(string left, string right)
        {
            return string.Equals(
                TrimDirectorySeparators(Path.GetFullPath(left)),
                TrimDirectorySeparators(Path.GetFullPath(right)),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string TrimDirectorySeparators(string path)
        {
            return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static string AppendDirectorySeparator(string path)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                return path;
            }

            return path + Path.DirectorySeparatorChar;
        }

        private static string NormalizeRelativePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
            }

            return path.Replace('\\', '/').Trim();
        }

        private static string ComputeSha1(string path)
        {
            using SHA1 sha1 = SHA1.Create();
            using FileStream stream = File.OpenRead(path);
            byte[] hash = sha1.ComputeHash(stream);
            StringBuilder builder = new StringBuilder(hash.Length * 2);
            foreach (byte value in hash)
            {
                builder.Append(value.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
