using System;
using System.Collections.Generic;
using PokeBlack2.Foundation.Runtime.Core;
using UnityEngine;

namespace PokeBlack2.Foundation.Runtime.Gen5.Contracts
{
    [CreateAssetMenu(menuName = "PokeBlack2/Gen5 Text Database", fileName = "Gen5TextDatabase")]
    public sealed class Gen5TextDatabaseAsset : ScriptableObject
    {
        [SerializeField] private string exportRoot = string.Empty;
        [SerializeField] private GameVersion gameVersion = GameVersion.PokemonBlackUsaEurope;
        [SerializeField] private string romFilename = string.Empty;
        [SerializeField] private string romSha1 = string.Empty;
        [SerializeField] private TextArchiveContract[] archives = Array.Empty<TextArchiveContract>();

        public string ExportRoot => exportRoot;
        public GameVersion GameVersion => gameVersion;
        public string RomFilename => romFilename;
        public string RomSha1 => romSha1;
        public IReadOnlyList<TextArchiveContract> Archives => archives;

        public int ArchiveCount => archives == null ? 0 : archives.Length;

        public int EntryCount
        {
            get
            {
                if (archives == null)
                {
                    return 0;
                }

                int entryCount = 0;
                foreach (TextArchiveContract archive in archives)
                {
                    if (archive?.Entries != null)
                    {
                        entryCount += archive.Entries.Length;
                    }
                }

                return entryCount;
            }
        }

        public int DecodedMessageCount
        {
            get
            {
                if (archives == null)
                {
                    return 0;
                }

                int messageCount = 0;
                foreach (TextArchiveContract archive in archives)
                {
                    if (archive?.Entries == null)
                    {
                        continue;
                    }

                    foreach (TextEntryContract entry in archive.Entries)
                    {
                        if (entry?.Messages != null)
                        {
                            messageCount += entry.Messages.Length;
                        }
                    }
                }

                return messageCount;
            }
        }

        public void Configure(
            string exportRoot,
            GameVersion gameVersion,
            string romFilename,
            string romSha1,
            TextArchiveContract[] archives)
        {
            this.exportRoot = exportRoot ?? string.Empty;
            this.gameVersion = gameVersion;
            this.romFilename = romFilename ?? string.Empty;
            this.romSha1 = romSha1 ?? string.Empty;
            this.archives = archives ?? Array.Empty<TextArchiveContract>();
        }

        public bool TryGetArchive(string archiveId, out TextArchiveContract archive)
        {
            if (archives != null)
            {
                foreach (TextArchiveContract candidate in archives)
                {
                    if (candidate != null &&
                        string.Equals(candidate.ArchiveId, archiveId, StringComparison.Ordinal))
                    {
                        archive = candidate;
                        return true;
                    }
                }
            }

            archive = null;
            return false;
        }

        public bool TryGetEntry(string archiveId, int bankIndex, out TextEntryContract entry)
        {
            if (!TryGetArchive(archiveId, out TextArchiveContract archive) ||
                archive?.Entries == null ||
                bankIndex < 0 ||
                bankIndex >= archive.Entries.Length)
            {
                entry = null;
                return false;
            }

            entry = archive.Entries[bankIndex];
            return entry != null;
        }

        public bool TryGetMessage(string archiveId, int bankIndex, int messageIndex, out TextMessageContract message)
        {
            if (!TryGetEntry(archiveId, bankIndex, out TextEntryContract entry) ||
                entry?.Messages == null ||
                messageIndex < 0 ||
                messageIndex >= entry.Messages.Length)
            {
                message = null;
                return false;
            }

            message = entry.Messages[messageIndex];
            return message != null;
        }

        public bool TryGetMessage(ScriptTextReferenceContract reference, out TextMessageContract message)
        {
            if (reference == null || !reference.IsValid())
            {
                message = null;
                return false;
            }

            return TryGetMessage(reference.ArchiveId, reference.BankIndex, reference.MessageIndex, out message);
        }
    }
}
