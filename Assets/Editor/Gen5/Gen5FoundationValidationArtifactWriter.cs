using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace PokeBlack2.Foundation.Editor
{
    public static class Gen5FoundationValidationArtifactWriter
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);

        public static Gen5FoundationValidationArtifactSet Write(Gen5FoundationValidationReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            if (string.IsNullOrWhiteSpace(report.RootPath))
            {
                throw new InvalidDataException("Validation report root path is required before writing artifacts.");
            }

            string resolvedRoot = Path.GetFullPath(report.RootPath);
            string logsRoot = Path.Combine(resolvedRoot, "logs", "foundation");
            Directory.CreateDirectory(logsRoot);

            string summaryPath = Path.Combine(resolvedRoot, Gen5ImportProfile.ValidationSummaryRelativePath.Replace('/', Path.DirectorySeparatorChar));
            string reportPath = Path.Combine(resolvedRoot, Gen5ImportProfile.ValidationReportRelativePath.Replace('/', Path.DirectorySeparatorChar));

            WriteText(summaryPath, report.FormatSummary());
            WriteJson(reportPath, report);

            return new Gen5FoundationValidationArtifactSet
            {
                RootPath = resolvedRoot,
                SummaryPath = summaryPath,
                ReportPath = reportPath,
            };
        }

        private static void WriteText(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content + Environment.NewLine, Utf8WithoutBom);
        }

        private static void WriteJson(string path, Gen5FoundationValidationReport report)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(
                typeof(Gen5FoundationValidationReport),
                new DataContractJsonSerializerSettings
                {
                    UseSimpleDictionaryFormat = true,
                });

            using MemoryStream stream = new MemoryStream();
            serializer.WriteObject(stream, report);
            File.WriteAllText(path, Utf8WithoutBom.GetString(stream.ToArray()), Utf8WithoutBom);
        }
    }

    [Serializable]
    public sealed class Gen5FoundationValidationArtifactSet
    {
        public string RootPath { get; set; } = string.Empty;
        public string SummaryPath { get; set; } = string.Empty;
        public string ReportPath { get; set; } = string.Empty;
    }
}
