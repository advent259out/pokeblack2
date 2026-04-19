using System;
using System.Collections.Generic;
using PokeBlack2.Foundation.Runtime.Core;
using UnityEngine;

namespace PokeBlack2.Foundation.Runtime.Gen5.Contracts
{
    [CreateAssetMenu(menuName = "PokeBlack2/Gen5 Script Database", fileName = "Gen5ScriptDatabase")]
    public sealed class Gen5ScriptDatabaseAsset : ScriptableObject
    {
        [SerializeField] private string exportRoot = string.Empty;
        [SerializeField] private GameVersion gameVersion = GameVersion.PokemonBlackUsaEurope;
        [SerializeField] private string romFilename = string.Empty;
        [SerializeField] private string romSha1 = string.Empty;
        [SerializeField] private ScriptProgramContract[] programs = Array.Empty<ScriptProgramContract>();

        public string ExportRoot => exportRoot;
        public GameVersion GameVersion => gameVersion;
        public string RomFilename => romFilename;
        public string RomSha1 => romSha1;
        public IReadOnlyList<ScriptProgramContract> Programs => programs;

        public int ProgramCount => programs == null ? 0 : programs.Length;

        public int ProcedureCount
        {
            get
            {
                if (programs == null)
                {
                    return 0;
                }

                int count = 0;
                foreach (ScriptProgramContract program in programs)
                {
                    if (program?.Procedures != null)
                    {
                        count += program.Procedures.Length;
                    }
                }

                return count;
            }
        }

        public int ParsedProcedureCount
        {
            get
            {
                if (programs == null)
                {
                    return 0;
                }

                int count = 0;
                foreach (ScriptProgramContract program in programs)
                {
                    if (program?.Procedures == null)
                    {
                        continue;
                    }

                    foreach (ScriptProcedureContract procedure in program.Procedures)
                    {
                        if (procedure != null &&
                            string.Equals(procedure.ParseStatus, "complete", StringComparison.Ordinal))
                        {
                            count += 1;
                        }
                    }
                }

                return count;
            }
        }

        public int DialogueLineCount
        {
            get
            {
                if (programs == null)
                {
                    return 0;
                }

                int count = 0;
                foreach (ScriptProgramContract program in programs)
                {
                    if (program?.DialogueLines != null)
                    {
                        count += program.DialogueLines.Length;
                    }
                }

                return count;
            }
        }

        public int ResolvedDialogueTextReferenceCount
        {
            get
            {
                if (programs == null)
                {
                    return 0;
                }

                int count = 0;
                foreach (ScriptProgramContract program in programs)
                {
                    if (program?.DialogueLines == null)
                    {
                        continue;
                    }

                    foreach (ScriptDialogueLineContract dialogueLine in program.DialogueLines)
                    {
                        if (dialogueLine?.Text != null && dialogueLine.Text.IsValid())
                        {
                            count += 1;
                        }
                    }
                }

                return count;
            }
        }

        public void Configure(
            string exportRoot,
            GameVersion gameVersion,
            string romFilename,
            string romSha1,
            ScriptProgramContract[] programs)
        {
            this.exportRoot = exportRoot ?? string.Empty;
            this.gameVersion = gameVersion;
            this.romFilename = romFilename ?? string.Empty;
            this.romSha1 = romSha1 ?? string.Empty;
            this.programs = programs ?? Array.Empty<ScriptProgramContract>();
        }

        public bool TryGetProgram(string archiveId, int memberIndex, out ScriptProgramContract program)
        {
            if (programs != null)
            {
                foreach (ScriptProgramContract candidate in programs)
                {
                    if (candidate != null &&
                        string.Equals(candidate.ArchiveId, archiveId, StringComparison.Ordinal) &&
                        candidate.MemberIndex == memberIndex)
                    {
                        program = candidate;
                        return true;
                    }
                }
            }

            program = null;
            return false;
        }
    }
}
