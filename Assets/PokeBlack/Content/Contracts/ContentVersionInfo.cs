using System;

namespace PokeBlack.Content.Contracts
{
    [Serializable]
    public sealed class ContentVersionInfo
    {
        public int SourceSchemaVersion { get; set; }

        public string ContentVersion { get; set; } = string.Empty;
    }
}
