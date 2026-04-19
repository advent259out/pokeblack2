using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace PokeBlack2.Foundation.Editor
{
    public static class Gen5FoundationValidationArtifactReader
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);

        public static Gen5FoundationValidationArtifactSnapshot LoadCanonical()
        {
            return LoadFromRoot(Gen5ImportProfile.CanonicalExportRoot);
        }

        public static Gen5FoundationValidationArtifactSnapshot LoadFromRoot(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Validation artifact root path cannot be null or whitespace.", nameof(rootPath));
            }

            string resolvedRoot = Path.GetFullPath(rootPath);
            string reportPath = ResolveReportPath(resolvedRoot);
            string summaryPath = ResolveSummaryPath(resolvedRoot);

            if (!File.Exists(reportPath))
            {
                throw new FileNotFoundException($"Validation report was not found at '{reportPath}'.", reportPath);
            }

            Gen5FoundationValidationReport report = ReadReport(reportPath);
            ValidateReport(report, resolvedRoot, reportPath);

            string summaryText = File.Exists(summaryPath)
                ? File.ReadAllText(summaryPath)
                : string.Empty;

            return new Gen5FoundationValidationArtifactSnapshot
            {
                RootPath = resolvedRoot,
                ReportPath = reportPath,
                SummaryPath = summaryPath,
                SummaryText = summaryText,
                Report = report,
            };
        }

        public static string ResolveReportPath(string rootPath)
        {
            return ResolvePath(rootPath, Gen5ImportProfile.ValidationReportRelativePath);
        }

        public static string ResolveSummaryPath(string rootPath)
        {
            return ResolvePath(rootPath, Gen5ImportProfile.ValidationSummaryRelativePath);
        }

        private static Gen5FoundationValidationReport ReadReport(string reportPath)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(
                typeof(Gen5FoundationValidationReport),
                new DataContractJsonSerializerSettings
                {
                    UseSimpleDictionaryFormat = true,
                });

            string json = File.ReadAllText(reportPath, Encoding.UTF8).TrimStart('\uFEFF');
            using MemoryStream stream = new MemoryStream(Utf8WithoutBom.GetBytes(json));
            object result = serializer.ReadObject(stream);
            if (result is Gen5FoundationValidationReport report)
            {
                return report;
            }

            throw new InvalidDataException(
                $"Validation report '{reportPath}' could not be deserialized as '{typeof(Gen5FoundationValidationReport).FullName}'.");
        }

        private static string ResolvePath(string rootPath, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Root path cannot be null or whitespace.", nameof(rootPath));
            }

            string resolvedRoot = Path.GetFullPath(rootPath);
            return Path.Combine(resolvedRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void ValidateReport(
            Gen5FoundationValidationReport report,
            string expectedRootPath,
            string reportPath)
        {
            if (report == null)
            {
                throw new InvalidDataException($"Validation report '{reportPath}' is required.");
            }

            if (string.IsNullOrWhiteSpace(report.RootPath))
            {
                throw new InvalidDataException($"Validation report '{reportPath}' is missing rootPath.");
            }

            if (report.GroupSummaries == null)
            {
                throw new InvalidDataException($"Validation report '{reportPath}' is missing groupSummaries.");
            }

            string reportRoot = Path.GetFullPath(report.RootPath);
            if (!string.Equals(reportRoot, expectedRootPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"Validation report root '{report.RootPath}' does not match the requested artifact root '{expectedRootPath}'.");
            }
        }
    }

    [Serializable]
    public sealed class Gen5FoundationValidationArtifactSnapshot
    {
        public string RootPath { get; set; } = string.Empty;
        public string ReportPath { get; set; } = string.Empty;
        public string SummaryPath { get; set; } = string.Empty;
        public string SummaryText { get; set; } = string.Empty;
        public Gen5FoundationValidationReport Report { get; set; }

        public string DisplaySummary
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(SummaryText))
                {
                    return SummaryText.TrimEnd();
                }

                return Report == null ? string.Empty : Report.FormatSummary();
            }
        }
    }
}
