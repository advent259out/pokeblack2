using System;
using System.Collections.Generic;
using System.IO;
using PokeBlack2.Foundation.Runtime.Core;
using PokeBlack2.Foundation.Runtime.Gen5.Contracts;
using UnityEditor;
using UnityEngine;

namespace PokeBlack2.Foundation.Editor
{
    public static class Gen5TextImportRunner
    {
        [MenuItem("PokeBlack2/Gen5/Import Text Metadata")]
        public static void ImportCanonicalFromMenu()
        {
            Gen5TextImportArtifactSet artifacts = ImportCanonical();
            Debug.Log(artifacts.FormatSummary());
            UnityEngine.Object importedAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(artifacts.TextDatabaseAssetPath);
            if (importedAsset != null)
            {
                EditorGUIUtility.PingObject(importedAsset);
            }
        }

        public static Gen5TextImportArtifactSet ImportCanonical()
        {
            return ImportFromRoot(Gen5ImportProfile.CanonicalExportRoot, Gen5ImportProfile.GeneratedAssetsRoot);
        }

        public static Gen5TextImportArtifactSet ImportFromRoot(string rootPath, string generatedAssetsRoot)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Text import root path cannot be null or whitespace.", nameof(rootPath));
            }

            if (string.IsNullOrWhiteSpace(generatedAssetsRoot))
            {
                throw new ArgumentException("Generated assets root cannot be null or whitespace.", nameof(generatedAssetsRoot));
            }

            string normalizedGeneratedAssetsRoot = NormalizeAssetPath(generatedAssetsRoot);
            Gen5FoundationImportSession session = Gen5FoundationImportSession.LoadFromRoot(rootPath);
            if (!session.HasGroup("text"))
            {
                throw new InvalidOperationException("The current export root does not contain a normalized 'text' group.");
            }

            IReadOnlyList<NormalizedSourceCatalogEntry> textSources = session.GetSourcesForGroup("text");
            if (textSources.Count == 0)
            {
                throw new InvalidDataException("The normalized 'text' group is present, but no text sources were registered.");
            }

            NormalizedTextGroupIndex groupIndex = session.LoadTextGroupIndex();
            TextArchiveContract[] archives = BuildArchives(textSources, groupIndex);
            string resourcesRoot = CombineAssetPath(normalizedGeneratedAssetsRoot, "Resources");
            string textDatabaseAssetPath = CombineAssetPath(resourcesRoot, "Imported/Gen5/Text/CanonicalGen5TextDatabase.asset");
            string profileAssetPath = CombineAssetPath(resourcesRoot, "Foundation/GameContentProfile.asset");

            EnsureAssetFolder(Path.GetDirectoryName(textDatabaseAssetPath)?.Replace('\\', '/'));
            EnsureAssetFolder(Path.GetDirectoryName(profileAssetPath)?.Replace('\\', '/'));

            Gen5TextDatabaseAsset textDatabase = LoadOrCreateAsset(
                textDatabaseAssetPath,
                () => ScriptableObject.CreateInstance<Gen5TextDatabaseAsset>());
            textDatabase.name = "CanonicalGen5TextDatabase";
            textDatabase.Configure(
                session.RootPath,
                GameVersion.PokemonBlackUsaEurope,
                session.RomInfo.Filename,
                session.RomInfo.Sha1,
                archives);
            EditorUtility.SetDirty(textDatabase);

            GameContentProfile profile = LoadOrCreateAsset(
                profileAssetPath,
                () => ScriptableObject.CreateInstance<GameContentProfile>());
            profile.name = "GameContentProfile";
            profile.ApplyImportedTextDatabase(textDatabase);
            EditorUtility.SetDirty(profile);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int entryCount = 0;
            int decodedMessageCount = 0;
            foreach (TextArchiveContract archive in archives)
            {
                entryCount += archive.Entries.Length;
                foreach (TextEntryContract entry in archive.Entries)
                {
                    decodedMessageCount += entry.Messages.Length;
                }
            }

            return new Gen5TextImportArtifactSet
            {
                RootPath = session.RootPath,
                GeneratedAssetsRoot = normalizedGeneratedAssetsRoot,
                ProfileAssetPath = profileAssetPath,
                TextDatabaseAssetPath = textDatabaseAssetPath,
                ArchiveCount = archives.Length,
                EntryCount = entryCount,
                DecodedMessageCount = decodedMessageCount,
            };
        }

        private static TextArchiveContract[] BuildArchives(
            IReadOnlyList<NormalizedSourceCatalogEntry> textSources,
            NormalizedTextGroupIndex groupIndex)
        {
            if (groupIndex.ContainerCount != textSources.Count)
            {
                throw new InvalidDataException(
                    $"Text group source count '{textSources.Count}' does not match text container count '{groupIndex.ContainerCount}'.");
            }

            Dictionary<string, NormalizedSourceCatalogEntry> sourcesByKey =
                new Dictionary<string, NormalizedSourceCatalogEntry>(StringComparer.Ordinal);
            foreach (NormalizedSourceCatalogEntry source in textSources)
            {
                sourcesByKey.Add(BuildContainerKey(source.FileId, source.Id, source.SourcePath), source);
            }

            int totalDecodedMessages = 0;
            TextArchiveContract[] archives = new TextArchiveContract[groupIndex.Containers.Count];
            for (int index = 0; index < groupIndex.Containers.Count; index++)
            {
                NormalizedTextContainer container = groupIndex.Containers[index];
                string key = BuildContainerKey(container.FileId, container.Id, container.SourcePath);
                if (!sourcesByKey.TryGetValue(key, out NormalizedSourceCatalogEntry source))
                {
                    throw new InvalidDataException(
                        $"Text container '{container.Id}' (fileId={container.FileId}) is missing a matching source-catalog entry.");
                }

                if (source.MemberCount != container.MemberCount)
                {
                    throw new InvalidDataException(
                        $"Text container '{container.Id}' member count '{container.MemberCount}' does not match source-catalog member count '{source.MemberCount}'.");
                }

                if (source.Size != container.Size)
                {
                    throw new InvalidDataException(
                        $"Text container '{container.Id}' size '{container.Size}' does not match source-catalog size '{source.Size}'.");
                }

                if (!string.Equals(source.Sha1, container.Sha1, StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        $"Text container '{container.Id}' sha1 '{container.Sha1}' does not match source-catalog sha1 '{source.Sha1}'.");
                }

                if (container.Members == null)
                {
                    throw new InvalidDataException($"Text container '{container.Id}' members list is required.");
                }

                if (container.MemberCount != container.Members.Count)
                {
                    throw new InvalidDataException(
                        $"Text container '{container.Id}' member count '{container.MemberCount}' does not match the decoded member entry count '{container.Members.Count}'.");
                }

                TextEntryContract[] entries = new TextEntryContract[container.Members.Count];
                int containerDecodedMessages = 0;
                for (int memberIndex = 0; memberIndex < container.Members.Count; memberIndex++)
                {
                    NormalizedTextBank member = container.Members[memberIndex];
                    ValidateTextBank(container.Id, member);
                    TextMessageContract[] messages = new TextMessageContract[member.Messages.Count];
                    for (int messageIndex = 0; messageIndex < member.Messages.Count; messageIndex++)
                    {
                        NormalizedTextMessage message = member.Messages[messageIndex];
                        ValidateTextMessage(container.Id, member.Index, message);
                        TextTokenContract[] tokens = new TextTokenContract[message.Tokens.Count];
                        for (int tokenIndex = 0; tokenIndex < message.Tokens.Count; tokenIndex++)
                        {
                            NormalizedTextToken token = message.Tokens[tokenIndex];
                            tokens[tokenIndex] = CreateTokenContract(container.Id, member.Index, message.EntryIndex, token);
                        }

                        messages[messageIndex] = new TextMessageContract
                        {
                            BlockIndex = message.BlockIndex,
                            CharCount = message.CharCount,
                            EntryIndex = message.EntryIndex,
                            Flags = message.Flags,
                            IsCompressed = message.IsCompressed,
                            Text = message.Text ?? string.Empty,
                            Tokens = tokens,
                        };
                    }

                    containerDecodedMessages += messages.Length;
                    entries[memberIndex] = new TextEntryContract
                    {
                        BlockCount = member.BlockCount,
                        Index = member.Index,
                        MessageCount = member.MessageCount,
                        MessagesPerBlock = member.MessagesPerBlock,
                        Messages = messages,
                        Sha1 = member.Sha1,
                        Size = member.Size,
                    };
                }

                if (container.DecodedMessageCount != containerDecodedMessages)
                {
                    throw new InvalidDataException(
                        $"Text container '{container.Id}' decoded message count '{container.DecodedMessageCount}' does not match the decoded message entry count '{containerDecodedMessages}'.");
                }

                totalDecodedMessages += containerDecodedMessages;
                archives[index] = new TextArchiveContract
                {
                    ArchiveId = container.Id,
                    ContainerType = container.ContainerType,
                    FileId = container.FileId,
                    SourcePath = container.SourcePath,
                    RawOutputPath = container.RawOutputPath,
                    Sha1 = container.Sha1,
                    ContainerSize = container.Size,
                    LargestMemberSize = container.LargestMemberSize,
                    MemberCount = container.MemberCount,
                    TotalMemberBytes = container.TotalMemberBytes,
                    Entries = entries,
                };
            }

            if (groupIndex.TotalDecodedMessages != totalDecodedMessages)
            {
                throw new InvalidDataException(
                    $"Text group decoded message count '{groupIndex.TotalDecodedMessages}' does not match the decoded message entry count '{totalDecodedMessages}'.");
            }

            return archives;
        }

        private static void ValidateTextBank(string archiveId, NormalizedTextBank bank)
        {
            if (bank == null)
            {
                throw new InvalidDataException($"Text bank in archive '{archiveId}' is required.");
            }

            if (bank.Messages == null)
            {
                throw new InvalidDataException($"Text bank '{bank.Index}' in archive '{archiveId}' requires a messages list.");
            }

            if (bank.MessageCount != bank.Messages.Count)
            {
                throw new InvalidDataException(
                    $"Text bank '{bank.Index}' in archive '{archiveId}' message count '{bank.MessageCount}' does not match the decoded message entry count '{bank.Messages.Count}'.");
            }

            if (bank.BlockCount <= 0)
            {
                throw new InvalidDataException($"Text bank '{bank.Index}' in archive '{archiveId}' must declare at least one block.");
            }

            if (bank.MessagesPerBlock < 0)
            {
                throw new InvalidDataException(
                    $"Text bank '{bank.Index}' in archive '{archiveId}' cannot declare a negative messagesPerBlock value.");
            }

            if ((bank.BlockCount * bank.MessagesPerBlock) != bank.MessageCount)
            {
                throw new InvalidDataException(
                    $"Text bank '{bank.Index}' in archive '{archiveId}' declares '{bank.BlockCount}' blocks and '{bank.MessagesPerBlock}' messagesPerBlock, but message count is '{bank.MessageCount}'.");
            }
        }

        private static void ValidateTextMessage(string archiveId, int bankIndex, NormalizedTextMessage message)
        {
            if (message == null)
            {
                throw new InvalidDataException($"Text message in archive '{archiveId}' bank '{bankIndex}' is required.");
            }

            if (message.Tokens == null)
            {
                throw new InvalidDataException(
                    $"Text message '{message.EntryIndex}' in archive '{archiveId}' bank '{bankIndex}' requires a tokens list.");
            }

            string rebuiltText = BuildRenderedText(message.Tokens);
            string normalizedText = message.Text ?? string.Empty;
            if (!string.Equals(rebuiltText, normalizedText, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Text message '{message.EntryIndex}' in archive '{archiveId}' bank '{bankIndex}' token rendering does not match the normalized text payload.");
            }
        }

        private static TextTokenContract CreateTokenContract(
            string archiveId,
            int bankIndex,
            int messageIndex,
            NormalizedTextToken token)
        {
            if (token == null)
            {
                throw new InvalidDataException(
                    $"Text token in archive '{archiveId}' bank '{bankIndex}' message '{messageIndex}' is required.");
            }

            string kind = token.Kind ?? string.Empty;
            if (string.IsNullOrWhiteSpace(kind))
            {
                throw new InvalidDataException(
                    $"Text token in archive '{archiveId}' bank '{bankIndex}' message '{messageIndex}' requires a non-empty kind.");
            }

            switch (kind)
            {
                case "text":
                    if (string.IsNullOrEmpty(token.Text))
                    {
                        throw new InvalidDataException(
                            $"Text token in archive '{archiveId}' bank '{bankIndex}' message '{messageIndex}' requires a non-empty text payload.");
                    }

                    break;

                case "lineBreak":
                case "pageBreak":
                case "carriageReturn":
                    break;

                case "variable":
                    if (!token.ControlCode.HasValue)
                    {
                        throw new InvalidDataException(
                            $"Variable token in archive '{archiveId}' bank '{bankIndex}' message '{messageIndex}' requires a controlCode.");
                    }

                    break;

                case "rawCodePoint":
                    if (!token.CodePoint.HasValue)
                    {
                        throw new InvalidDataException(
                            $"rawCodePoint token in archive '{archiveId}' bank '{bankIndex}' message '{messageIndex}' requires a codePoint.");
                    }

                    break;

                default:
                    throw new InvalidDataException(
                        $"Unsupported token kind '{kind}' in archive '{archiveId}' bank '{bankIndex}' message '{messageIndex}'.");
            }

            return new TextTokenContract
            {
                Arguments = token.Arguments == null ? Array.Empty<int>() : token.Arguments.ToArray(),
                CodePoint = token.CodePoint ?? -1,
                ControlCode = token.ControlCode ?? -1,
                Kind = kind,
                Text = token.Text ?? string.Empty,
            };
        }

        private static string BuildRenderedText(IReadOnlyList<NormalizedTextToken> tokens)
        {
            if (tokens == null || tokens.Count == 0)
            {
                return string.Empty;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            foreach (NormalizedTextToken token in tokens)
            {
                builder.Append(RenderToken(token));
            }

            return builder.ToString();
        }

        private static string RenderToken(NormalizedTextToken token)
        {
            string kind = token.Kind ?? string.Empty;
            switch (kind)
            {
                case "text":
                    return token.Text ?? string.Empty;

                case "lineBreak":
                    return "\\n";

                case "pageBreak":
                    return "\\f";

                case "carriageReturn":
                    return "\\r";

                case "variable":
                    List<string> arguments = new List<string>();
                    if (token.ControlCode.HasValue)
                    {
                        arguments.Add(token.ControlCode.Value.ToString());
                    }

                    if (token.Arguments != null)
                    {
                        foreach (int argument in token.Arguments)
                        {
                            arguments.Add(argument.ToString());
                        }
                    }

                    return $"VAR({string.Join(", ", arguments)})";

                case "rawCodePoint":
                    return token.CodePoint.HasValue ? $"\\x{token.CodePoint.Value:X4}" : string.Empty;

                default:
                    throw new InvalidDataException($"Unsupported token kind '{kind}'.");
            }
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
    }

    [Serializable]
    public sealed class Gen5TextImportArtifactSet
    {
        public string RootPath { get; set; } = string.Empty;
        public string GeneratedAssetsRoot { get; set; } = string.Empty;
        public string ProfileAssetPath { get; set; } = string.Empty;
        public string TextDatabaseAssetPath { get; set; } = string.Empty;
        public int ArchiveCount { get; set; }
        public int EntryCount { get; set; }
        public int DecodedMessageCount { get; set; }

        public string FormatSummary()
        {
            return
                $"Imported {ArchiveCount} text archives, {EntryCount} text banks, and {DecodedMessageCount} decoded text messages from '{RootPath}' into '{TextDatabaseAssetPath}' and '{ProfileAssetPath}'.";
        }
    }
}
