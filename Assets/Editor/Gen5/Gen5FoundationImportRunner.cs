using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PokeBlack2.Foundation.Editor
{
    public static class Gen5FoundationImportRunner
    {
        [MenuItem("PokeBlack2/Gen5/Validate Foundation Export")]
        public static void ValidateCanonicalFromMenu()
        {
            Gen5FoundationValidationReport report = ValidateCanonical();
            Gen5FoundationValidationArtifactSet artifacts = Gen5FoundationValidationArtifactWriter.Write(report);
            Debug.Log(report.FormatSummary());
            Debug.Log($"Validation artifacts written to '{artifacts.ReportPath}' and '{artifacts.SummaryPath}'.");
            Gen5FoundationValidationWindow.OpenWindowAndLoadArtifacts(artifacts.RootPath);
        }

        public static Gen5FoundationValidationReport ValidateCanonical()
        {
            return ValidateFromRoot(Gen5ImportProfile.CanonicalExportRoot);
        }

        public static Gen5FoundationValidationArtifactSet ValidateCanonicalAndWriteArtifacts()
        {
            return ValidateAndWriteArtifacts(Gen5ImportProfile.CanonicalExportRoot);
        }

        public static Gen5FoundationValidationReport ValidateFromRoot(string rootPath)
        {
            Gen5FoundationImportSession session = Gen5FoundationImportSession.LoadFromRoot(rootPath);
            Gen5FoundationValidationReport report = new Gen5FoundationValidationReport
            {
                RootPath = session.RootPath,
                Game = session.Manifest.Game,
                RomFilename = session.RomInfo.Filename,
                RomSha1 = session.RomInfo.Sha1,
                RomSize = session.RomInfo.Size,
                SourceCount = session.SourceCatalog.SourceCount,
            };

            foreach (string groupName in Gen5ImportProfile.GetSupportedNormalizedGroups())
            {
                int sourceCount = session.GetSourcesForGroup(groupName).Count;
                bool isAvailable = session.HasGroup(groupName);
                if (sourceCount > 0 && !isAvailable)
                {
                    throw new InvalidDataException(
                        $"Normalized group '{groupName}' has {sourceCount} source entries but no registered group index output.");
                }

                int containerCount = 0;
                if (isAvailable)
                {
                    NormalizedGroupIndex groupIndex = session.LoadGroupIndex(groupName);
                    containerCount = groupIndex.ContainerCount;
                    if (containerCount != sourceCount)
                    {
                        throw new InvalidDataException(
                            $"Normalized group '{groupName}' source count '{sourceCount}' does not match group index container count '{containerCount}'.");
                    }
                }

                report.GroupSummaries.Add(
                    new Gen5FoundationGroupValidationSummary
                    {
                        GroupName = groupName,
                        IsAvailable = isAvailable,
                        SourceCount = sourceCount,
                        ContainerCount = containerCount,
                    });
            }

            return report;
        }

        public static Gen5FoundationValidationArtifactSet ValidateAndWriteArtifacts(string rootPath)
        {
            Gen5FoundationValidationReport report = ValidateFromRoot(rootPath);
            return Gen5FoundationValidationArtifactWriter.Write(report);
        }
    }
}
