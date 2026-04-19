using System;
using System.IO;
using UnityEngine;

namespace PokeBlack2.Foundation.Runtime.Core
{
    public static class RuntimeContentAccessGuard
    {
        private static readonly string[] RestrictedRoots =
        {
            "ROMs",
            Path.Combine("External", "Exports"),
        };

        public static void EnsurePathAllowed(string candidatePath)
        {
            if (string.IsNullOrWhiteSpace(candidatePath))
            {
                return;
            }

            string fullCandidatePath = NormalizeAgainstProjectRoot(candidatePath);
            foreach (string restrictedRoot in RestrictedRoots)
            {
                string fullRestrictedRoot = NormalizeAgainstProjectRoot(restrictedRoot);
                if (fullCandidatePath.StartsWith(fullRestrictedRoot, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Runtime access to '{candidatePath}' is forbidden. Offline content must stay behind the import seam.");
                }
            }
        }

        private static string NormalizeAgainstProjectRoot(string path)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string combined = Path.IsPathRooted(path) ? path : Path.Combine(projectRoot, path);
            return Path.GetFullPath(combined)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}

