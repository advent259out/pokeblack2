using System;
using System.Collections.Generic;
using System.IO;
using PokeBlack.Content.Runtime;
using PokeBlack2.Foundation.Runtime.Core;
using PokeBlack2.Foundation.Runtime.Gen5.Contracts;
using UnityEditor;
using UnityEngine;

namespace PokeBlack2.Foundation.Editor
{
    public static class Gen5ScriptImportRunner
    {
        private const int SpecialMessageIdFloor = 0x8000;

        [MenuItem("PokeBlack2/Gen5/Import Script Metadata")]
        public static void ImportCanonicalFromMenu()
        {
            Gen5ScriptImportArtifactSet artifacts = ImportCanonical();
            Debug.Log(artifacts.FormatSummary());
            UnityEngine.Object importedAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(artifacts.ScriptDatabaseAssetPath);
            if (importedAsset != null)
            {
                EditorGUIUtility.PingObject(importedAsset);
            }
        }

        public static Gen5ScriptImportArtifactSet ImportCanonical()
        {
            return ImportFromRoot(Gen5ImportProfile.CanonicalExportRoot, Gen5ImportProfile.GeneratedAssetsRoot);
        }

        public static Gen5ScriptImportArtifactSet ImportFromRoot(string rootPath, string generatedAssetsRoot)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Script import root path cannot be null or whitespace.", nameof(rootPath));
            }

            if (string.IsNullOrWhiteSpace(generatedAssetsRoot))
            {
                throw new ArgumentException("Generated assets root cannot be null or whitespace.", nameof(generatedAssetsRoot));
            }

            string normalizedGeneratedAssetsRoot = NormalizeAssetPath(generatedAssetsRoot);
            Gen5FoundationImportSession session = Gen5FoundationImportSession.LoadFromRoot(rootPath);
            if (!session.HasGroup("scripts"))
            {
                throw new InvalidOperationException("The current export root does not contain a normalized 'scripts' group.");
            }

            IReadOnlyList<NormalizedSourceCatalogEntry> scriptSources = session.GetSourcesForGroup("scripts");
            if (scriptSources.Count == 0)
            {
                throw new InvalidDataException("The normalized 'scripts' group is present, but no script sources were registered.");
            }

            NormalizedScriptGroupIndex groupIndex = session.LoadScriptGroupIndex();
            IReadOnlyDictionary<int, ScriptTextBindingTarget> textBindingsByMember = BuildScriptTextBindingsByMember(session);
            IReadOnlyDictionary<string, IReadOnlyDictionary<int, int>> textMessageCountsByArchive = BuildTextMessageCountsByArchive(session);
            ScriptProgramContract[] programs = BuildPrograms(scriptSources, groupIndex, textBindingsByMember, textMessageCountsByArchive);
            string resourcesRoot = CombineAssetPath(normalizedGeneratedAssetsRoot, "Resources");
            string scriptDatabaseAssetPath = CombineAssetPath(resourcesRoot, "Imported/Gen5/Scripts/CanonicalGen5ScriptDatabase.asset");
            string profileAssetPath = CombineAssetPath(resourcesRoot, "Foundation/GameContentProfile.asset");

            EnsureAssetFolder(Path.GetDirectoryName(scriptDatabaseAssetPath)?.Replace('\\', '/'));
            EnsureAssetFolder(Path.GetDirectoryName(profileAssetPath)?.Replace('\\', '/'));

            Gen5ScriptDatabaseAsset scriptDatabase = LoadOrCreateAsset(
                scriptDatabaseAssetPath,
                () => ScriptableObject.CreateInstance<Gen5ScriptDatabaseAsset>());
            scriptDatabase.name = "CanonicalGen5ScriptDatabase";
            scriptDatabase.Configure(
                session.RootPath,
                GameVersion.PokemonBlackUsaEurope,
                session.RomInfo.Filename,
                session.RomInfo.Sha1,
                programs);
            EditorUtility.SetDirty(scriptDatabase);

            GameContentProfile profile = LoadOrCreateAsset(
                profileAssetPath,
                () => ScriptableObject.CreateInstance<GameContentProfile>());
            profile.name = "GameContentProfile";
            ContentManifest contentManifest = ContentManifestImportUtility.ImportForSession(session, normalizedGeneratedAssetsRoot);
            profile.ApplyContentManifest(contentManifest);
            profile.ApplyImportedScriptDatabase(scriptDatabase);
            EditorUtility.SetDirty(profile);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return new Gen5ScriptImportArtifactSet
            {
                RootPath = session.RootPath,
                GeneratedAssetsRoot = normalizedGeneratedAssetsRoot,
                ProfileAssetPath = profileAssetPath,
                ContentManifestAssetPath = AssetDatabase.GetAssetPath(contentManifest),
                ContentVersion = contentManifest.ContentVersion,
                ScriptDatabaseAssetPath = scriptDatabaseAssetPath,
                ProgramCount = programs.Length,
                ProcedureCount = CountProcedures(programs),
                ParsedProcedureCount = CountParsedProcedures(programs),
                DialogueLineCount = CountDialogueLines(programs),
                ResolvedDialogueTextReferenceCount = CountResolvedDialogueTextReferences(programs),
            };
        }

        private static ScriptProgramContract[] BuildPrograms(
            IReadOnlyList<NormalizedSourceCatalogEntry> scriptSources,
            NormalizedScriptGroupIndex groupIndex,
            IReadOnlyDictionary<int, ScriptTextBindingTarget> textBindingsByMember,
            IReadOnlyDictionary<string, IReadOnlyDictionary<int, int>> textMessageCountsByArchive)
        {
            if (groupIndex.ContainerCount != scriptSources.Count)
            {
                throw new InvalidDataException(
                    $"Script group source count '{scriptSources.Count}' does not match script container count '{groupIndex.ContainerCount}'.");
            }

            Dictionary<string, NormalizedSourceCatalogEntry> sourcesByKey =
                new Dictionary<string, NormalizedSourceCatalogEntry>(StringComparer.Ordinal);
            foreach (NormalizedSourceCatalogEntry source in scriptSources)
            {
                sourcesByKey.Add(BuildContainerKey(source.FileId, source.Id, source.SourcePath), source);
            }

            List<ScriptProgramContract> programs = new List<ScriptProgramContract>();
            foreach (NormalizedScriptContainer container in groupIndex.Containers)
            {
                string key = BuildContainerKey(container.FileId, container.Id, container.SourcePath);
                if (!sourcesByKey.TryGetValue(key, out NormalizedSourceCatalogEntry source))
                {
                    throw new InvalidDataException(
                        $"Script container '{container.Id}' (fileId={container.FileId}) is missing a matching source-catalog entry.");
                }

                ValidateScriptContainer(source, container);
                foreach (NormalizedScriptFile member in container.Members)
                {
                    ValidateScriptFile(container.Id, member);
                    programs.Add(CreateProgramContract(container, member, textBindingsByMember, textMessageCountsByArchive));
                }
            }

            return programs.ToArray();
        }

        private static void ValidateScriptContainer(NormalizedSourceCatalogEntry source, NormalizedScriptContainer container)
        {
            if (source.MemberCount != container.MemberCount)
            {
                throw new InvalidDataException(
                    $"Script container '{container.Id}' member count '{container.MemberCount}' does not match source-catalog member count '{source.MemberCount}'.");
            }

            if (source.Size != container.Size)
            {
                throw new InvalidDataException(
                    $"Script container '{container.Id}' size '{container.Size}' does not match source-catalog size '{source.Size}'.");
            }

            if (!string.Equals(source.Sha1, container.Sha1, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Script container '{container.Id}' sha1 '{container.Sha1}' does not match source-catalog sha1 '{source.Sha1}'.");
            }

            if (container.Members == null)
            {
                throw new InvalidDataException($"Script container '{container.Id}' members list is required.");
            }

            if (container.MemberCount != container.Members.Count)
            {
                throw new InvalidDataException(
                    $"Script container '{container.Id}' member count '{container.MemberCount}' does not match the decoded member entry count '{container.Members.Count}'.");
            }
        }

        private static void ValidateScriptFile(string archiveId, NormalizedScriptFile member)
        {
            if (member == null)
            {
                throw new InvalidDataException($"Script file in archive '{archiveId}' is required.");
            }

            int headerEntryCount = member.HeaderEntries == null ? 0 : member.HeaderEntries.Count;
            int procedureCount = member.Procedures == null ? 0 : member.Procedures.Count;
            int dialogueLineCount = member.DialogueLines == null ? 0 : member.DialogueLines.Count;
            int parseWarningCount = member.ParseWarnings == null ? 0 : member.ParseWarnings.Count;

            if (member.HeaderEntryCount != headerEntryCount)
            {
                throw new InvalidDataException(
                    $"Script file '{member.Index}' in archive '{archiveId}' header entry count '{member.HeaderEntryCount}' does not match the decoded header entry count '{headerEntryCount}'.");
            }

            if (member.ProcedureCount != procedureCount)
            {
                throw new InvalidDataException(
                    $"Script file '{member.Index}' in archive '{archiveId}' procedure count '{member.ProcedureCount}' does not match the decoded procedure entry count '{procedureCount}'.");
            }

            if (member.DialogueLineCount != dialogueLineCount)
            {
                throw new InvalidDataException(
                    $"Script file '{member.Index}' in archive '{archiveId}' dialogue line count '{member.DialogueLineCount}' does not match the decoded dialogue line entry count '{dialogueLineCount}'.");
            }

            if (member.ParseWarningCount != parseWarningCount)
            {
                throw new InvalidDataException(
                    $"Script file '{member.Index}' in archive '{archiveId}' parse warning count '{member.ParseWarningCount}' does not match the parse warning entry count '{parseWarningCount}'.");
            }

            int parsedProcedureCount = 0;
            foreach (NormalizedScriptProcedure procedure in EnumerateOrEmpty(member.Procedures))
            {
                ValidateScriptProcedure(archiveId, member.Index, procedure);
                if (string.Equals(procedure.ParseStatus, "complete", StringComparison.Ordinal))
                {
                    parsedProcedureCount += 1;
                }
            }

            if (member.ParsedProcedureCount != parsedProcedureCount)
            {
                throw new InvalidDataException(
                    $"Script file '{member.Index}' in archive '{archiveId}' parsed procedure count '{member.ParsedProcedureCount}' does not match the decoded complete procedure count '{parsedProcedureCount}'.");
            }
        }

        private static void ValidateScriptProcedure(string archiveId, int memberIndex, NormalizedScriptProcedure procedure)
        {
            if (procedure == null)
            {
                throw new InvalidDataException($"Script procedure in archive '{archiveId}' member '{memberIndex}' is required.");
            }

            int instructionCount = procedure.Instructions == null ? 0 : procedure.Instructions.Count;
            int dialogueLineCount = procedure.DialogueLines == null ? 0 : procedure.DialogueLines.Count;

            if (procedure.InstructionCount != instructionCount)
            {
                throw new InvalidDataException(
                    $"Script procedure '{procedure.ProcedureId}' in archive '{archiveId}' member '{memberIndex}' instruction count '{procedure.InstructionCount}' does not match the decoded instruction entry count '{instructionCount}'.");
            }

            if (procedure.DialogueLineCount != dialogueLineCount)
            {
                throw new InvalidDataException(
                    $"Script procedure '{procedure.ProcedureId}' in archive '{archiveId}' member '{memberIndex}' dialogue line count '{procedure.DialogueLineCount}' does not match the decoded dialogue line entry count '{dialogueLineCount}'.");
            }
        }

        private static ScriptProgramContract CreateProgramContract(
            NormalizedScriptContainer container,
            NormalizedScriptFile member,
            IReadOnlyDictionary<int, ScriptTextBindingTarget> textBindingsByMember,
            IReadOnlyDictionary<string, IReadOnlyDictionary<int, int>> textMessageCountsByArchive)
        {
            HashSet<string> operationTokens = new HashSet<string>(StringComparer.Ordinal);
            List<NormalizedScriptProcedure> sourceProcedures = member.Procedures ?? new List<NormalizedScriptProcedure>();
            ScriptProcedureContract[] procedures = new ScriptProcedureContract[sourceProcedures.Count];
            for (int procedureIndex = 0; procedureIndex < sourceProcedures.Count; procedureIndex++)
            {
                NormalizedScriptProcedure sourceProcedure = sourceProcedures[procedureIndex];
                List<NormalizedScriptInstruction> sourceInstructions = sourceProcedure.Instructions ?? new List<NormalizedScriptInstruction>();
                ScriptInstructionContract[] instructions = new ScriptInstructionContract[sourceInstructions.Count];
                for (int instructionIndex = 0; instructionIndex < sourceInstructions.Count; instructionIndex++)
                {
                    NormalizedScriptInstruction sourceInstruction = sourceInstructions[instructionIndex];
                    instructions[instructionIndex] = new ScriptInstructionContract
                    {
                        Offset = sourceInstruction.Offset,
                        Opcode = sourceInstruction.Opcode,
                        Mnemonic = sourceInstruction.Mnemonic ?? string.Empty,
                        ByteLength = sourceInstruction.ByteLength,
                        Operands = sourceInstruction.Operands == null ? Array.Empty<int>() : sourceInstruction.Operands.ToArray(),
                        BranchTargetOffset = sourceInstruction.BranchTargetOffset ?? -1,
                    };
                    if (!string.IsNullOrWhiteSpace(instructions[instructionIndex].Mnemonic))
                    {
                        operationTokens.Add(instructions[instructionIndex].Mnemonic);
                    }
                }

                procedures[procedureIndex] = new ScriptProcedureContract
                {
                    ProcedureId = sourceProcedure.ProcedureId ?? string.Empty,
                    EntryKind = sourceProcedure.EntryKind ?? string.Empty,
                    HeaderIndex = sourceProcedure.HeaderIndex ?? -1,
                    StartOffset = sourceProcedure.StartOffset,
                    EndOffset = sourceProcedure.EndOffset,
                    ParseStatus = sourceProcedure.ParseStatus ?? string.Empty,
                    Instructions = instructions,
                };
            }

            List<NormalizedScriptHeaderEntry> sourceHeaderEntries = member.HeaderEntries ?? new List<NormalizedScriptHeaderEntry>();
            ScriptHeaderEntryContract[] headerEntries = new ScriptHeaderEntryContract[sourceHeaderEntries.Count];
            for (int index = 0; index < sourceHeaderEntries.Count; index++)
            {
                NormalizedScriptHeaderEntry sourceEntry = sourceHeaderEntries[index];
                headerEntries[index] = new ScriptHeaderEntryContract
                {
                    HeaderIndex = sourceEntry.HeaderIndex,
                    HeaderOffset = sourceEntry.HeaderOffset,
                    StoredOffset = sourceEntry.StoredOffset,
                    StartOffset = sourceEntry.StartOffset,
                };
            }

            List<NormalizedScriptDialogueLine> sourceDialogueLines = member.DialogueLines ?? new List<NormalizedScriptDialogueLine>();
            ScriptDialogueLineContract[] dialogueLines = new ScriptDialogueLineContract[sourceDialogueLines.Count];
            for (int index = 0; index < sourceDialogueLines.Count; index++)
            {
                NormalizedScriptDialogueLine sourceLine = sourceDialogueLines[index];
                dialogueLines[index] = new ScriptDialogueLineContract
                {
                    LineId = sourceLine.LineId ?? string.Empty,
                    ProcedureId = sourceLine.ProcedureId ?? string.Empty,
                    InstructionOffset = sourceLine.InstructionOffset,
                    Command = sourceLine.Command ?? string.Empty,
                    MessageId = sourceLine.MessageId,
                    SpeakerObjectId = sourceLine.SpeakerObjectId ?? -1,
                    ViewType = sourceLine.ViewType ?? -1,
                    MessageType = sourceLine.MessageType ?? -1,
                    VariantA = sourceLine.VariantA ?? -1,
                    VariantB = sourceLine.VariantB ?? -1,
                    Text = CreateTextReference(member.Index, sourceLine, textBindingsByMember, textMessageCountsByArchive),
                };
            }

            string[] orderedOperationTokens = new List<string>(operationTokens).ToArray();
            Array.Sort(orderedOperationTokens, StringComparer.Ordinal);

            return new ScriptProgramContract
            {
                ProgramId = $"{container.Id}:{member.Index}",
                ArchiveId = container.Id,
                MemberIndex = member.Index,
                MemberSha1 = member.Sha1 ?? string.Empty,
                MemberSize = member.Size,
                HeaderMarkerOffset = member.HeaderMarkerOffset ?? -1,
                HeaderEntries = headerEntries,
                Procedures = procedures,
                ParseWarnings = member.ParseWarnings == null ? Array.Empty<string>() : member.ParseWarnings.ToArray(),
                OperationTokens = orderedOperationTokens,
                DialogueLines = dialogueLines,
            };
        }

        private static ScriptTextReferenceContract CreateTextReference(
            int memberIndex,
            NormalizedScriptDialogueLine sourceLine,
            IReadOnlyDictionary<int, ScriptTextBindingTarget> textBindingsByMember,
            IReadOnlyDictionary<string, IReadOnlyDictionary<int, int>> textMessageCountsByArchive)
        {
            ScriptTextReferenceContract reference = new ScriptTextReferenceContract();
            if (sourceLine == null ||
                textBindingsByMember == null ||
                !textBindingsByMember.TryGetValue(memberIndex, out ScriptTextBindingTarget binding))
            {
                return reference;
            }

            if (!textMessageCountsByArchive.TryGetValue(binding.TextArchiveId, out IReadOnlyDictionary<int, int> bankCounts) ||
                !bankCounts.TryGetValue(binding.TextBankIndex, out int messageCount))
            {
                throw new InvalidDataException(
                    $"Script text binding for script member '{memberIndex}' targets missing text archive '{binding.TextArchiveId}' bank '{binding.TextBankIndex}'.");
            }

            if (sourceLine.MessageId < 0)
            {
                return reference;
            }

            if (sourceLine.MessageId >= messageCount)
            {
                if (sourceLine.MessageId >= SpecialMessageIdFloor)
                {
                    return reference;
                }

                throw new InvalidDataException(
                    $"Script dialogue line '{sourceLine.LineId}' references message '{sourceLine.MessageId}', but archive '{binding.TextArchiveId}' bank '{binding.TextBankIndex}' only contains '{messageCount}' messages.");
            }

            reference.ArchiveId = binding.TextArchiveId;
            reference.BankIndex = binding.TextBankIndex;
            reference.MessageIndex = sourceLine.MessageId;
            return reference;
        }

        private static IReadOnlyDictionary<string, IReadOnlyDictionary<int, int>> BuildTextMessageCountsByArchive(
            Gen5FoundationImportSession session)
        {
            Dictionary<string, IReadOnlyDictionary<int, int>> result =
                new Dictionary<string, IReadOnlyDictionary<int, int>>(StringComparer.Ordinal);
            if (!session.HasGroup("text"))
            {
                return result;
            }

            NormalizedTextGroupIndex textGroupIndex = session.LoadTextGroupIndex();
            foreach (NormalizedTextContainer container in textGroupIndex.Containers)
            {
                Dictionary<int, int> bankCounts = new Dictionary<int, int>();
                foreach (NormalizedTextBank member in container.Members ?? new List<NormalizedTextBank>())
                {
                    bankCounts.Add(member.Index, member.MessageCount);
                }

                result.Add(container.Id, bankCounts);
            }

            return result;
        }

        private static IReadOnlyDictionary<int, ScriptTextBindingTarget> BuildScriptTextBindingsByMember(
            Gen5FoundationImportSession session)
        {
            Dictionary<int, ScriptTextBindingTarget> result = new Dictionary<int, ScriptTextBindingTarget>();
            if (!session.HasGroup("maps") || !session.HasGroup("text"))
            {
                return result;
            }

            IReadOnlyDictionary<string, IReadOnlyDictionary<int, int>> textMessageCountsByArchive = BuildTextMessageCountsByArchive(session);
            NormalizedMapGroupIndex mapGroupIndex = session.LoadMapGroupIndex();
            foreach (NormalizedMapContainer container in mapGroupIndex.Containers)
            {
                foreach (NormalizedMapScriptTextBinding binding in container.ScriptTextBindings ?? new List<NormalizedMapScriptTextBinding>())
                {
                    ValidateScriptTextBinding(binding, textMessageCountsByArchive);
                    ScriptTextBindingTarget target = new ScriptTextBindingTarget(
                        binding.TextArchiveId,
                        binding.TextBankIndex,
                        binding.ZoneIndex);
                    if (result.TryGetValue(binding.ScriptMemberIndex, out ScriptTextBindingTarget existingTarget))
                    {
                        if (!existingTarget.Equals(target))
                        {
                            throw new InvalidDataException(
                                $"Script member '{binding.ScriptMemberIndex}' resolves to conflicting text targets '{existingTarget.TextArchiveId}:{existingTarget.TextBankIndex}' and '{target.TextArchiveId}:{target.TextBankIndex}'.");
                        }

                        continue;
                    }

                    result.Add(binding.ScriptMemberIndex, target);
                }
            }

            return result;
        }

        private static void ValidateScriptTextBinding(
            NormalizedMapScriptTextBinding binding,
            IReadOnlyDictionary<string, IReadOnlyDictionary<int, int>> textMessageCountsByArchive)
        {
            if (binding == null)
            {
                throw new InvalidDataException("Normalized script text binding entries are required.");
            }

            if (binding.ScriptMemberIndex < 0)
            {
                throw new InvalidDataException("Normalized script text bindings require a non-negative script member index.");
            }

            if (string.IsNullOrWhiteSpace(binding.TextArchiveId))
            {
                throw new InvalidDataException(
                    $"Script text binding for script member '{binding.ScriptMemberIndex}' requires a non-empty text archive id.");
            }

            if (!textMessageCountsByArchive.TryGetValue(binding.TextArchiveId, out IReadOnlyDictionary<int, int> bankCounts))
            {
                throw new InvalidDataException(
                    $"Script text binding for script member '{binding.ScriptMemberIndex}' references unknown text archive '{binding.TextArchiveId}'.");
            }

            if (!bankCounts.ContainsKey(binding.TextBankIndex))
            {
                throw new InvalidDataException(
                    $"Script text binding for script member '{binding.ScriptMemberIndex}' references missing text bank '{binding.TextBankIndex}' in archive '{binding.TextArchiveId}'.");
            }
        }

        private static int CountProcedures(IReadOnlyList<ScriptProgramContract> programs)
        {
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

        private static int CountParsedProcedures(IReadOnlyList<ScriptProgramContract> programs)
        {
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

        private static int CountDialogueLines(IReadOnlyList<ScriptProgramContract> programs)
        {
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

        private static int CountResolvedDialogueTextReferences(IReadOnlyList<ScriptProgramContract> programs)
        {
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

        private static T LoadOrCreateAsset<T>(string assetPath, Func<T> createAsset)
            where T : ScriptableObject
        {
            T existingAsset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existingAsset != null)
            {
                return existingAsset;
            }

            T asset = createAsset();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static void EnsureAssetFolder(string assetFolderPath)
        {
            if (string.IsNullOrWhiteSpace(assetFolderPath))
            {
                throw new ArgumentException("Asset folder path cannot be null or whitespace.", nameof(assetFolderPath));
            }

            string normalizedPath = NormalizeAssetPath(assetFolderPath);
            if (!normalizedPath.StartsWith("Assets", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Asset folder '{assetFolderPath}' must stay under the Unity Assets root.");
            }

            string[] segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            string currentPath = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string nextPath = $"{currentPath}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, segments[index]);
                }

                currentPath = nextPath;
            }
        }

        private static string NormalizeAssetPath(string assetPath)
        {
            return assetPath.Replace('\\', '/').TrimEnd('/');
        }

        private static string CombineAssetPath(string left, string right)
        {
            return NormalizeAssetPath($"{NormalizeAssetPath(left)}/{NormalizeAssetPath(right)}");
        }

        private static string BuildContainerKey(int fileId, string id, string sourcePath)
        {
            return $"{fileId}:{id}:{sourcePath}";
        }

        private static IEnumerable<T> EnumerateOrEmpty<T>(IEnumerable<T> values)
        {
            return values ?? Array.Empty<T>();
        }
    }

    [Serializable]
    public sealed class Gen5ScriptImportArtifactSet
    {
        public string RootPath { get; set; } = string.Empty;
        public string GeneratedAssetsRoot { get; set; } = string.Empty;
        public string ProfileAssetPath { get; set; } = string.Empty;
        public string ContentManifestAssetPath { get; set; } = string.Empty;
        public string ContentVersion { get; set; } = string.Empty;
        public string ScriptDatabaseAssetPath { get; set; } = string.Empty;
        public int ProgramCount { get; set; }
        public int ProcedureCount { get; set; }
        public int ParsedProcedureCount { get; set; }
        public int DialogueLineCount { get; set; }
        public int ResolvedDialogueTextReferenceCount { get; set; }

        public string FormatSummary()
        {
            return
                $"Imported {ProgramCount} script programs, {ProcedureCount} decoded procedures, {ParsedProcedureCount} fully parsed procedures, {DialogueLineCount} dialogue lines, and {ResolvedDialogueTextReferenceCount} resolved text references from '{RootPath}' into '{ScriptDatabaseAssetPath}', '{ProfileAssetPath}', and '{ContentManifestAssetPath}' (contentVersion={ContentVersion}).";
        }
    }

    internal readonly struct ScriptTextBindingTarget : IEquatable<ScriptTextBindingTarget>
    {
        public ScriptTextBindingTarget(string textArchiveId, int textBankIndex, int zoneIndex)
        {
            TextArchiveId = textArchiveId ?? string.Empty;
            TextBankIndex = textBankIndex;
            ZoneIndex = zoneIndex;
        }

        public string TextArchiveId { get; }
        public int TextBankIndex { get; }
        public int ZoneIndex { get; }

        public bool Equals(ScriptTextBindingTarget other)
        {
            return string.Equals(TextArchiveId, other.TextArchiveId, StringComparison.Ordinal) &&
                   TextBankIndex == other.TextBankIndex &&
                   ZoneIndex == other.ZoneIndex;
        }
    }
}
