using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace PokeBlack2.Foundation.Editor
{
    [Serializable]
    [DataContract]
    public sealed class Gen5FoundationValidationReport
    {
        [DataMember(Name = "rootPath")]
        public string RootPath { get; set; } = string.Empty;

        [DataMember(Name = "game")]
        public string Game { get; set; } = string.Empty;

        [DataMember(Name = "romFilename")]
        public string RomFilename { get; set; } = string.Empty;

        [DataMember(Name = "romSha1")]
        public string RomSha1 { get; set; } = string.Empty;

        [DataMember(Name = "romSize")]
        public long RomSize { get; set; }

        [DataMember(Name = "sourceCount")]
        public int SourceCount { get; set; }

        [DataMember(Name = "groupSummaries")]
        public List<Gen5FoundationGroupValidationSummary> GroupSummaries { get; set; } =
            new List<Gen5FoundationGroupValidationSummary>();

        public int AvailableGroupCount
        {
            get
            {
                int count = 0;
                foreach (Gen5FoundationGroupValidationSummary groupSummary in GroupSummaries)
                {
                    if (groupSummary.IsAvailable)
                    {
                        count += 1;
                    }
                }

                return count;
            }
        }

        public string FormatSummary()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Gen5 foundation validation passed for '{RootPath}'.");
            builder.AppendLine($"Game: {Game}");
            builder.AppendLine($"ROM: {RomFilename} ({RomSha1}, {RomSize} bytes)");
            builder.AppendLine($"Sources: {SourceCount}");
            builder.AppendLine($"Available groups: {AvailableGroupCount}/{GroupSummaries.Count}");

            foreach (Gen5FoundationGroupValidationSummary groupSummary in GroupSummaries)
            {
                string availability = groupSummary.IsAvailable ? "available" : "missing";
                builder.AppendLine(
                    $"- {groupSummary.GroupName}: {availability}, sources={groupSummary.SourceCount}, containers={groupSummary.ContainerCount}");
            }

            return builder.ToString().TrimEnd();
        }
    }

    [Serializable]
    [DataContract]
    public sealed class Gen5FoundationGroupValidationSummary
    {
        [DataMember(Name = "groupName")]
        public string GroupName { get; set; } = string.Empty;

        [DataMember(Name = "isAvailable")]
        public bool IsAvailable { get; set; }

        [DataMember(Name = "sourceCount")]
        public int SourceCount { get; set; }

        [DataMember(Name = "containerCount")]
        public int ContainerCount { get; set; }
    }
}
