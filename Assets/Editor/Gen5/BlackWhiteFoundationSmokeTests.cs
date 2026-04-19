using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using PokeBlack.Content.Contracts;
using PokeBlack.Content.Runtime;
using PokeBlack2.Foundation.Runtime.Bootstrap;
using PokeBlack2.Foundation.Runtime.Core;
using PokeBlack2.Foundation.Runtime.Gen5.Contracts;
using PokeBlack2.Foundation.Runtime.Gen5.Text;
using UnityEditor;
using UnityEngine;

namespace PokeBlack2.Foundation.Editor
{
    public sealed class BlackWhiteFoundationSmokeTests
    {
        private const string FixtureRoot = "Assets/Editor/Gen5/TestData/EmptyM0Fixture";
        private const string FixtureOutput = "Assets/Editor/Gen5/TestData/EmptyM0Fixture/normalized/metadata/contract-placeholder.json";
        private const string FixtureRomInfoOutput = "Assets/Editor/Gen5/TestData/EmptyM0Fixture/normalized/metadata/rom-info.json";
        private const string FixtureSourceCatalogOutput = "Assets/Editor/Gen5/TestData/EmptyM0Fixture/normalized/metadata/source-catalog.json";
        private const string FixtureTextOutput = "Assets/Editor/Gen5/TestData/EmptyM0Fixture/normalized/text/index.json";

        [Test]
        public void Registry_Loads_EmptyFixtureManifest()
        {
            NormalizedContractRegistry registry = NormalizedContractRegistry.LoadFromRoot(FixtureRoot);

            Assert.That(registry.Manifest.Game, Is.EqualTo(Gen5ImportProfile.GameId));
            Assert.That(registry.Manifest.SchemaVersion, Is.EqualTo(Gen5ImportProfile.SchemaVersion));
            Assert.That(registry.ContainsOutput(FixtureOutput), Is.True);
            Assert.That(registry.ContainsRootRelativeOutput(Gen5ImportProfile.ContractPlaceholderRelativePath), Is.True);
            Assert.That(registry.ContainsRootRelativeOutput(Gen5ImportProfile.RomInfoRelativePath), Is.True);
            Assert.That(registry.ContainsRootRelativeOutput(Gen5ImportProfile.SourceCatalogRelativePath), Is.True);
            Assert.That(registry.ContainsOutput(FixtureRomInfoOutput), Is.True);
            Assert.That(registry.ContainsOutput(FixtureSourceCatalogOutput), Is.True);
            Assert.That(registry.ContainsOutput(FixtureTextOutput), Is.True);
        }

        [Test]
        public void Registry_Rejects_InvalidSchemaVersion()
        {
            string root = CreateTemporaryExportRoot();
            Directory.CreateDirectory(Path.Combine(root, "normalized", "metadata"));
            File.WriteAllText(Path.Combine(root, "normalized", "metadata", "contract-placeholder.json"), "{ }");
            File.WriteAllText(
                Path.Combine(root, "manifests", "manifest.json"),
                "{\"schemaVersion\":999,\"game\":\"pokemon-black\",\"rom\":{\"filename\":\"pokeblack.nds\",\"sha1\":\"a68b3bedf5c1e53556e41e59cdf396c20b331896\",\"size\":268435456},\"exportRoot\":\"Temp/InvalidFixture\",\"generatedAt\":\"2026-04-18T00:00:00Z\",\"normalizedOutputs\":[],\"hashes\":{}}");

            Assert.Throws<InvalidDataException>(() => NormalizedContractRegistry.LoadFromRoot(root));
        }

        [Test]
        public void Registry_LoadJson_Returns_NormalizedContractPayload()
        {
            NormalizedContractRegistry registry = NormalizedContractRegistry.LoadFromRoot(FixtureRoot);

            NormalizedRomInfo romInfo = registry.LoadRomInfo();
            NormalizedSourceCatalog sourceCatalog = registry.LoadSourceCatalog();
            NormalizedGroupIndex textIndex = registry.LoadGroupIndex("text");

            Assert.That(romInfo.Filename, Is.EqualTo("pokeblack.nds"));
            Assert.That(romInfo.Sha1, Is.EqualTo("a68b3bedf5c1e53556e41e59cdf396c20b331896"));
            Assert.That(sourceCatalog.SourceCount, Is.EqualTo(0));
            Assert.That(sourceCatalog.Rom.Game, Is.EqualTo("Pokemon Black Workspace Baseline"));
            Assert.That(textIndex.Group, Is.EqualTo("text"));
            Assert.That(textIndex.ContainerCount, Is.EqualTo(0));
        }

        [Test]
        public void ImportSession_Loads_Fixture_Through_Typed_Metadata_Seam()
        {
            Gen5FoundationImportSession session = Gen5FoundationImportSession.LoadFromRoot(FixtureRoot);

            Assert.That(session.Manifest.Game, Is.EqualTo(Gen5ImportProfile.GameId));
            Assert.That(session.RomInfo.Filename, Is.EqualTo("pokeblack.nds"));
            Assert.That(session.SourceCatalog.SourceCount, Is.EqualTo(0));
            Assert.That(session.HasGroup("text"), Is.True);
            Assert.That(session.HasGroup("maps"), Is.False);
            Assert.That(session.AvailableGroups, Does.Contain("text"));
            Assert.That(session.GetSourcesForGroup("text"), Is.Empty);
            Assert.That(session.LoadGroupIndex("text").Group, Is.EqualTo("text"));
        }

        [Test]
        public void ValidationRunner_Builds_Fixture_Report()
        {
            Gen5FoundationValidationReport report = Gen5FoundationImportRunner.ValidateFromRoot(FixtureRoot);

            Assert.That(report.Game, Is.EqualTo(Gen5ImportProfile.GameId));
            Assert.That(report.RomFilename, Is.EqualTo("pokeblack.nds"));
            Assert.That(report.AvailableGroupCount, Is.EqualTo(1));
            Assert.That(report.GroupSummaries, Has.Count.EqualTo(Gen5ImportProfile.GetSupportedNormalizedGroups().Count));
            Assert.That(report.GroupSummaries[0].GroupName, Is.EqualTo("text"));
            Assert.That(report.GroupSummaries[0].IsAvailable, Is.True);
            Assert.That(report.GroupSummaries[0].SourceCount, Is.EqualTo(0));
            Assert.That(report.FormatSummary(), Does.Contain("Available groups: 1/7"));
        }

        [Test]
        public void ValidationRunner_Writes_Report_Artifacts()
        {
            string root = CreateTemporaryImportSessionRoot(
                romInfoJson: CreateRomInfoJson(),
                sourceCatalogJson: CreateSourceCatalogJson(Array.Empty<string>()),
                groupOutputs: new Dictionary<string, string>
                {
                    { Gen5ImportProfile.GetGroupIndexRelativePath("text"), CreateGroupIndexJson("text", 0) },
                });

            Gen5FoundationValidationArtifactSet artifacts = Gen5FoundationImportRunner.ValidateAndWriteArtifacts(root);

            Assert.That(File.Exists(artifacts.ReportPath), Is.True);
            Assert.That(File.Exists(artifacts.SummaryPath), Is.True);
            Assert.That(artifacts.ReportPath.Replace('\\', '/'), Does.EndWith("logs/foundation/validation-report.json"));
            Assert.That(artifacts.SummaryPath.Replace('\\', '/'), Does.EndWith("logs/foundation/validation-summary.txt"));
            Assert.That(File.ReadAllText(artifacts.SummaryPath), Does.Contain("Available groups: 1/7"));
            Assert.That(File.ReadAllText(artifacts.ReportPath), Does.Contain("\"groupSummaries\""));
            Assert.That(File.ReadAllText(artifacts.ReportPath), Does.Contain("\"groupName\":\"text\""));
            Assert.That(File.ReadAllBytes(artifacts.ReportPath)[0], Is.EqualTo((byte)'{'));
            Assert.That(File.ReadAllBytes(artifacts.SummaryPath)[0], Is.EqualTo((byte)'G'));
        }

        [Test]
        public void ValidationArtifactReader_Loads_Written_Report_Artifacts()
        {
            string root = CreateTemporaryImportSessionRoot(
                romInfoJson: CreateRomInfoJson(),
                sourceCatalogJson: CreateSourceCatalogJson(Array.Empty<string>()),
                groupOutputs: new Dictionary<string, string>
                {
                    { Gen5ImportProfile.GetGroupIndexRelativePath("text"), CreateGroupIndexJson("text", 0) },
                });

            Gen5FoundationImportRunner.ValidateAndWriteArtifacts(root);
            Gen5FoundationValidationArtifactSnapshot snapshot = Gen5FoundationValidationArtifactReader.LoadFromRoot(root);

            Assert.That(snapshot.RootPath, Is.EqualTo(Path.GetFullPath(root)));
            Assert.That(snapshot.ReportPath, Does.EndWith("logs\\foundation\\validation-report.json").Or.EndWith("logs/foundation/validation-report.json"));
            Assert.That(snapshot.SummaryPath, Does.EndWith("logs\\foundation\\validation-summary.txt").Or.EndWith("logs/foundation/validation-summary.txt"));
            Assert.That(snapshot.Report.AvailableGroupCount, Is.EqualTo(1));
            Assert.That(snapshot.DisplaySummary, Does.Contain("Available groups: 1/7"));
        }

        [Test]
        public void ValidationArtifactReader_Rejects_Report_With_Mismatched_Root()
        {
            string root = CreateTemporaryImportSessionRoot(
                romInfoJson: CreateRomInfoJson(),
                sourceCatalogJson: CreateSourceCatalogJson(Array.Empty<string>()),
                groupOutputs: new Dictionary<string, string>
                {
                    { Gen5ImportProfile.GetGroupIndexRelativePath("text"), CreateGroupIndexJson("text", 0) },
                });

            Gen5FoundationValidationArtifactSet artifacts = Gen5FoundationImportRunner.ValidateAndWriteArtifacts(root);
            File.WriteAllText(
                artifacts.ReportPath,
                CreateValidationReportJson("Temp/BlackWhiteFoundationTests/SomeOtherRoot"));

            Assert.Throws<InvalidDataException>(() => Gen5FoundationValidationArtifactReader.LoadFromRoot(root));
        }

        [Test]
        public void ImportSession_Rejects_SourceCatalog_Count_Mismatch()
        {
            string root = CreateTemporaryImportSessionRoot(
                romInfoJson:
                    "{" +
                    "\"fileCount\":0,\"filename\":\"pokeblack.nds\",\"game\":\"Pokemon Black Workspace Baseline\",\"namedFileCount\":0," +
                    "\"sha1\":\"a68b3bedf5c1e53556e41e59cdf396c20b331896\",\"size\":268435456,\"unnamedFileCount\":0" +
                    "}",
                sourceCatalogJson:
                    "{" +
                    "\"rom\":{\"filename\":\"pokeblack.nds\",\"game\":\"Pokemon Black Workspace Baseline\",\"sha1\":\"a68b3bedf5c1e53556e41e59cdf396c20b331896\",\"size\":268435456}," +
                    "\"sourceCount\":1,\"sources\":[]" +
                    "}");

            Assert.Throws<InvalidDataException>(() => Gen5FoundationImportSession.LoadFromRoot(root));
        }

        [Test]
        public void ValidationRunner_Rejects_Group_WithSources_But_NoOutput()
        {
            string root = CreateTemporaryImportSessionRoot(
                romInfoJson: CreateRomInfoJson(),
                sourceCatalogJson: CreateSourceCatalogJson(new[]
                {
                    CreateSourceCatalogEntryJson("text", "system-text", "a/0/0/2"),
                }));

            Assert.Throws<InvalidDataException>(() => Gen5FoundationImportRunner.ValidateFromRoot(root));
        }

        [Test]
        public void ImportSession_Rejects_Group_Index_Count_Mismatch()
        {
            string root = CreateTemporaryImportSessionRoot(
                romInfoJson: CreateRomInfoJson(),
                sourceCatalogJson: CreateSourceCatalogJson(new[]
                {
                    CreateSourceCatalogEntryJson("text", "system-text", "a/0/0/2"),
                }),
                groupOutputs: new Dictionary<string, string>
                {
                    {
                        Gen5ImportProfile.GetGroupIndexRelativePath("text"),
                        CreateGroupIndexJson("text", 0)
                    },
                });

            Assert.Throws<InvalidDataException>(() => Gen5FoundationImportRunner.ValidateFromRoot(root));
        }

        [Test]
        public void Registry_Rejects_UnsupportedNormalizedGroupName()
        {
            NormalizedContractRegistry registry = NormalizedContractRegistry.LoadFromRoot(FixtureRoot);

            Assert.Throws<ArgumentException>(() => registry.LoadGroupIndex("audio"));
        }

        [Test]
        public void Registry_Rejects_MismatchedExportRootBinding()
        {
            string root = CreateTemporaryExportRoot();
            string placeholderPath = Path.Combine(root, "normalized", "metadata", "contract-placeholder.json");
            File.WriteAllText(placeholderPath, "{ }");
            string placeholderRelativePath = placeholderPath.Replace('\\', '/');
            string placeholderHash = ComputeSha1(placeholderPath);

            File.WriteAllText(
                Path.Combine(root, "manifests", "manifest.json"),
                CreateManifestJson(
                    "Temp/BlackWhiteFoundationTests/SomeOtherRoot",
                    placeholderRelativePath,
                    placeholderHash));

            Assert.Throws<InvalidDataException>(() => NormalizedContractRegistry.LoadFromRoot(root));
        }

        [Test]
        public void Registry_Rejects_HashMismatch_OnRegisteredOutput()
        {
            string root = CreateTemporaryExportRoot();
            string placeholderPath = Path.Combine(root, "normalized", "metadata", "contract-placeholder.json");
            File.WriteAllText(placeholderPath, "{ }");
            string placeholderRelativePath = placeholderPath.Replace('\\', '/');

            File.WriteAllText(
                Path.Combine(root, "manifests", "manifest.json"),
                CreateManifestJson(
                    root.Replace('\\', '/'),
                    placeholderRelativePath,
                    "not-the-real-hash"));

            Assert.Throws<InvalidDataException>(() => NormalizedContractRegistry.LoadFromRoot(root));
        }

        [Test]
        public void Registry_Rejects_ParentTraversal_InManifestOutputPath()
        {
            string root = CreateTemporaryExportRoot();
            string placeholderPath = Path.Combine(root, "normalized", "metadata", "contract-placeholder.json");
            File.WriteAllText(placeholderPath, "{ }");

            File.WriteAllText(
                Path.Combine(root, "manifests", "manifest.json"),
                CreateManifestJson(
                    root.Replace('\\', '/'),
                    "../outside.json",
                    "deadbeef"));

            Assert.Throws<InvalidDataException>(() => NormalizedContractRegistry.LoadFromRoot(root));
        }

        [Test]
        public void ExportRootLayout_Rejects_UnexpectedTopLevelDirectory()
        {
            string root = CreateTemporaryExportRoot();
            Directory.CreateDirectory(Path.Combine(root, "unexpected"));

            Assert.Throws<InvalidDataException>(() => ExtractionManifestValidator.ValidateExportRootLayout(root));
        }

        [Test]
        public void RuntimeAssemblyDefinition_DoesNotReferenceEditorAssembly()
        {
            string asmdefPath = "Assets/Scripts/PokeBlack2.Runtime.asmdef";
            string asmdefJson = File.ReadAllText(asmdefPath);

            Assert.That(asmdefJson.Contains("PokeBlack2.Gen5.Editor", StringComparison.Ordinal), Is.False);
            Assert.That(asmdefJson.Contains("UnityEditor", StringComparison.Ordinal), Is.False);
        }

        [Test]
        public void RuntimeSources_DoNotReference_UnityEditor()
        {
            foreach (string file in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(file);
                Assert.That(content.Contains("using UnityEditor", StringComparison.Ordinal), Is.False, $"Unexpected UnityEditor import in {file}");
                Assert.That(content.Contains("UnityEditor.", StringComparison.Ordinal), Is.False, $"Unexpected UnityEditor usage in {file}");
            }
        }

        [Test]
        public void FoundationBootstrap_Loads_DefaultProfile_WhenNoAssetExists()
        {
            GameObject gameObject = new GameObject("FoundationBootstrapTest");
            try
            {
                FoundationBootstrap bootstrap = gameObject.AddComponent<FoundationBootstrap>();
                GameContentProfile profile = bootstrap.Initialize();

                Assert.That(profile, Is.Not.Null);
                Assert.That(profile.GameVersion, Is.EqualTo(GameVersion.PokemonBlackUsaEurope));
                Assert.That(profile.StrictOfflineBoundaries, Is.True);
                Assert.That(profile.HasContentManifest, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RuntimeContentAccessGuard_Rejects_RestrictedRoots()
        {
            Assert.Throws<InvalidOperationException>(() => RuntimeContentAccessGuard.EnsurePathAllowed("ROMs/pokeblack.nds"));
            Assert.Throws<InvalidOperationException>(() => RuntimeContentAccessGuard.EnsurePathAllowed("External/Exports/BlackWhite/M0"));
            Assert.DoesNotThrow(() => RuntimeContentAccessGuard.EnsurePathAllowed("Assets/Resources"));
        }

        [Test]
        public void TextImportRunner_Imports_Canonical_Text_Metadata_Assets()
        {
            string root = CreateCanonicalTextImportSessionRoot();
            string generatedAssetsRoot = CreateTemporaryGeneratedAssetsRoot();

            try
            {
                Gen5TextImportArtifactSet artifacts = Gen5TextImportRunner.ImportFromRoot(
                    root,
                    generatedAssetsRoot);

                Gen5TextDatabaseAsset textDatabase = AssetDatabase.LoadAssetAtPath<Gen5TextDatabaseAsset>(artifacts.TextDatabaseAssetPath);
                GameContentProfile profile = AssetDatabase.LoadAssetAtPath<GameContentProfile>(artifacts.ProfileAssetPath);
                ContentManifest contentManifest = AssertImportedContentManifest(
                    profile,
                    artifacts.ContentManifestAssetPath,
                    artifacts.ContentVersion,
                    expectedPresentGroups: new[] { "text" },
                    expectedAbsentGroups: new[] { "maps", "scripts" });

                Assert.That(textDatabase, Is.Not.Null);
                Assert.That(profile, Is.Not.Null);
                Assert.That(profile.LoadTextDatabase(), Is.SameAs(textDatabase));
                Assert.That(artifacts.ContentManifestAssetPath, Is.EqualTo(CreateGeneratedContentManifestAssetPath(generatedAssetsRoot)));
                Assert.That(contentManifest.AvailableGroups, Is.EqualTo(new[] { "text" }));
                Assert.That(textDatabase.ArchiveCount, Is.EqualTo(2));
                Assert.That(textDatabase.EntryCount, Is.EqualTo(760));
                Assert.That(textDatabase.DecodedMessageCount, Is.EqualTo(56700));
                Assert.That(textDatabase.TryGetArchive("system-text", out TextArchiveContract systemText), Is.True);
                Assert.That(systemText.MemberCount, Is.EqualTo(288));
                Assert.That(systemText.Entries[0].Index, Is.EqualTo(0));
                Assert.That(systemText.Entries[0].Size, Is.GreaterThan(0));
                Assert.That(systemText.Entries[0].Messages[0].Text, Is.EqualTo("No Answer"));
                Assert.That(systemText.Entries[176].Messages[1].Text, Is.EqualTo("Cheren"));
                Assert.That(systemText.Entries[176].Messages[1].IsCompressed, Is.True);
                Assert.That(systemText.Entries[176].Messages[1].Tokens, Has.Length.EqualTo(1));
                Assert.That(systemText.Entries[176].Messages[1].Tokens[0].Kind, Is.EqualTo("text"));
                Assert.That(systemText.Entries[176].Messages[1].Tokens[0].Text, Is.EqualTo("Cheren"));
                Assert.That(systemText.Entries[3].Messages[56].Tokens, Has.Length.EqualTo(7));
                Assert.That(systemText.Entries[3].Messages[56].Tokens[1].Kind, Is.EqualTo("lineBreak"));
                Assert.That(systemText.Entries[3].Messages[56].Tokens[2].Kind, Is.EqualTo("variable"));
                Assert.That(systemText.Entries[3].Messages[56].Tokens[2].ControlCode, Is.EqualTo(256));
                Assert.That(systemText.Entries[3].Messages[56].Tokens[2].Arguments, Is.EqualTo(new[] { 0 }));
                Assert.That(systemText.Entries[3].Messages[56].Tokens[4].Kind, Is.EqualTo("pageBreak"));
                Assert.That(systemText.Entries[3].Messages[62].Tokens[4].Kind, Is.EqualTo("carriageReturn"));
                Assert.That(textDatabase.TryGetArchive("event-text", out TextArchiveContract eventText), Is.True);
                Assert.That(eventText.MemberCount, Is.EqualTo(472));
                Assert.That(eventText.Entries[0].Messages[0].Text, Is.Not.Empty);
                Assert.That(artifacts.FormatSummary(), Does.Contain("Imported 2 text archives, 760 text banks, and 56700 decoded text messages"));
            }
            finally
            {
                DeleteAssetTree(generatedAssetsRoot);
            }
        }

        [Test]
        public void TextImportRunner_Rejects_Preexisting_ContentManifest_With_Unsupported_Schema()
        {
            string root = CreateCanonicalTextImportSessionRoot();
            string generatedAssetsRoot = CreateTemporaryGeneratedAssetsRoot();

            try
            {
                CreateGeneratedContentManifestAsset(
                    generatedAssetsRoot,
                    new ContentManifestData
                    {
                        SchemaVersion = 999,
                        Version = new ContentVersionInfo
                        {
                            SourceSchemaVersion = Gen5ImportProfile.SchemaVersion,
                            ContentVersion = "0123456789abcdef0123456789abcdef01234567",
                        },
                        GameId = Gen5ImportProfile.GameId,
                        ContractFamily = GameContentProfile.DefaultContractFamily,
                        ProfileId = GameContentProfile.DefaultProfileId,
                        RomFilename = "pokeblack.nds",
                        RomSha1 = "a68b3bedf5c1e53556e41e59cdf396c20b331896",
                        RomSize = 268435456,
                        SourceGeneratedAt = "2026-04-18T00:00:00Z",
                        AvailableGroups = new[] { "text" },
                    });

                Assert.Throws<InvalidDataException>(() => Gen5TextImportRunner.ImportFromRoot(root, generatedAssetsRoot));
            }
            finally
            {
                DeleteAssetTree(generatedAssetsRoot);
            }
        }

        [Test]
        public void TextImportRunner_Produces_Stable_ContentVersion_For_Identical_Input()
        {
            string root = CreateCanonicalTextImportSessionRoot();
            string generatedAssetsRootA = CreateTemporaryGeneratedAssetsRoot();
            string generatedAssetsRootB = CreateTemporaryGeneratedAssetsRoot();

            try
            {
                Gen5TextImportArtifactSet artifactsA = Gen5TextImportRunner.ImportFromRoot(root, generatedAssetsRootA);
                Gen5TextImportArtifactSet artifactsB = Gen5TextImportRunner.ImportFromRoot(root, generatedAssetsRootB);

                Assert.That(artifactsA.ContentVersion, Is.EqualTo(artifactsB.ContentVersion));
                Assert.That(artifactsA.ContentVersion, Has.Length.EqualTo(40));
                Assert.That(artifactsA.ContentManifestAssetPath, Is.Not.EqualTo(artifactsB.ContentManifestAssetPath));
            }
            finally
            {
                DeleteAssetTree(generatedAssetsRootA);
                DeleteAssetTree(generatedAssetsRootB);
            }
        }

        [Test]
        public void TextImportRunner_Rejects_Missing_Text_Group()
        {
            string root = CreateTemporaryImportSessionRoot(
                romInfoJson: CreateRomInfoJson(),
                sourceCatalogJson: CreateSourceCatalogJson(Array.Empty<string>()));
            string generatedAssetsRoot = CreateTemporaryGeneratedAssetsRoot();

            try
            {
                Assert.Throws<InvalidOperationException>(() => Gen5TextImportRunner.ImportFromRoot(root, generatedAssetsRoot));
            }
            finally
            {
                DeleteAssetTree(generatedAssetsRoot);
            }
        }

        [Test]
        public void TextImportRunner_Rejects_Text_Bank_With_Mismatched_Decoded_Message_Count()
        {
            string root = CreateTemporaryImportSessionRoot(
                romInfoJson: CreateRomInfoJson(),
                sourceCatalogJson: CreateSourceCatalogJson(new[]
                {
                    CreateSourceCatalogEntryJson("text", "system-text", "a/0/0/2"),
                }),
                groupOutputs: new Dictionary<string, string>
                {
                    {
                        Gen5ImportProfile.GetGroupIndexRelativePath("text"),
                        CreateDecodedTextGroupIndexJson()
                    },
                });
            string generatedAssetsRoot = CreateTemporaryGeneratedAssetsRoot();

            try
            {
                Assert.Throws<InvalidDataException>(() => Gen5TextImportRunner.ImportFromRoot(root, generatedAssetsRoot));
            }
            finally
            {
                DeleteAssetTree(generatedAssetsRoot);
            }
        }

        [Test]
        public void TextImportRunner_Rejects_Text_Message_With_Token_Render_Mismatch()
        {
            string root = CreateTemporaryImportSessionRoot(
                romInfoJson: CreateRomInfoJson(),
                sourceCatalogJson: CreateSourceCatalogJson(new[]
                {
                    CreateSourceCatalogEntryJson("text", "system-text", "a/0/0/2"),
                }),
                groupOutputs: new Dictionary<string, string>
                {
                    {
                        Gen5ImportProfile.GetGroupIndexRelativePath("text"),
                        CreateDecodedTextGroupIndexJsonWithTokenMismatch()
                    },
                });
            string generatedAssetsRoot = CreateTemporaryGeneratedAssetsRoot();

            try
            {
                Assert.Throws<InvalidDataException>(() => Gen5TextImportRunner.ImportFromRoot(root, generatedAssetsRoot));
            }
            finally
            {
                DeleteAssetTree(generatedAssetsRoot);
            }
        }

        [Test]
        public void ScriptImportRunner_Imports_Canonical_Script_Metadata_Assets()
        {
            string root = CreateCanonicalScriptImportSessionRoot();
            string generatedAssetsRoot = CreateTemporaryGeneratedAssetsRoot();

            try
            {
                Gen5TextImportRunner.ImportFromRoot(root, generatedAssetsRoot);
                Gen5ScriptImportArtifactSet artifacts = Gen5ScriptImportRunner.ImportFromRoot(root, generatedAssetsRoot);
                Gen5TextDatabaseAsset textDatabase = AssetDatabase.LoadAssetAtPath<Gen5TextDatabaseAsset>(Gen5ImportProfile.CanonicalTextDatabaseAssetPath.Replace("Assets/Generated", generatedAssetsRoot));
                Gen5ScriptDatabaseAsset scriptDatabase = AssetDatabase.LoadAssetAtPath<Gen5ScriptDatabaseAsset>(artifacts.ScriptDatabaseAssetPath);
                GameContentProfile profile = AssetDatabase.LoadAssetAtPath<GameContentProfile>(artifacts.ProfileAssetPath);
                ContentManifest contentManifest = AssertImportedContentManifest(
                    profile,
                    artifacts.ContentManifestAssetPath,
                    artifacts.ContentVersion,
                    expectedPresentGroups: new[] { "maps", "scripts", "text" },
                    expectedAbsentGroups: new[] { "pokemon" });

                Assert.That(textDatabase, Is.Not.Null);
                Assert.That(scriptDatabase, Is.Not.Null);
                Assert.That(profile, Is.Not.Null);
                Assert.That(profile.LoadTextDatabase(), Is.SameAs(textDatabase));
                Assert.That(profile.LoadScriptDatabase(), Is.SameAs(scriptDatabase));
                Assert.That(artifacts.ContentManifestAssetPath, Is.EqualTo(CreateGeneratedContentManifestAssetPath(generatedAssetsRoot)));
                Assert.That(contentManifest.AvailableGroups, Is.EqualTo(new[] { "maps", "scripts", "text" }));
                Assert.That(scriptDatabase.ProgramCount, Is.EqualTo(899));
                Assert.That(scriptDatabase.ProcedureCount, Is.EqualTo(3317));
                Assert.That(scriptDatabase.ParsedProcedureCount, Is.EqualTo(535));
                Assert.That(scriptDatabase.DialogueLineCount, Is.EqualTo(997));
                Assert.That(scriptDatabase.ResolvedDialogueTextReferenceCount, Is.EqualTo(948));
                Assert.That(scriptDatabase.TryGetProgram("script-containers", 0, out ScriptProgramContract memberZero), Is.True);
                Assert.That(memberZero.HeaderEntries[0].StartOffset, Is.EqualTo(16));
                Assert.That(memberZero.Procedures[0].Instructions[0].Mnemonic, Is.EqualTo("LockAll"));
                Assert.That(memberZero.Procedures[0].Instructions[1].Mnemonic, Is.EqualTo("PrepareSoundCue"));
                Assert.That(memberZero.Procedures[0].Instructions[2].Mnemonic, Is.EqualTo("FacePlayer"));
                Assert.That(scriptDatabase.TryGetProgram("script-containers", 2, out ScriptProgramContract memberTwo), Is.True);
                Assert.That(memberTwo.DialogueLines.Length, Is.GreaterThan(0));
                Assert.That(memberTwo.DialogueLines[0].Command, Is.EqualTo("message2"));
                Assert.That(memberTwo.DialogueLines[0].Text.ArchiveId, Is.EqualTo("event-text"));
                Assert.That(memberTwo.DialogueLines[0].Text.BankIndex, Is.EqualTo(3));
                Assert.That(memberTwo.DialogueLines[0].Text.MessageIndex, Is.EqualTo(0));
                Assert.That(textDatabase.TryGetMessage(memberTwo.DialogueLines[0].Text, out TextMessageContract resolvedMessage), Is.True);
                Assert.That(
                    resolvedMessage.Text,
                    Is.EqualTo("Many people come here from White Forest.\\r\\nIf you enter White Forest and invite\\npeople, they might move here."));
                Assert.That(artifacts.FormatSummary(), Does.Contain("Imported 899 script programs"));
                Assert.That(artifacts.FormatSummary(), Does.Contain("948 resolved text references"));
            }
            finally
            {
                DeleteAssetTree(generatedAssetsRoot);
            }
        }

        [Test]
        public void WorldImportRunner_Imports_Canonical_World_Metadata_Assets()
        {
            string root = CreateCanonicalWorldImportSessionRoot();
            string generatedAssetsRoot = CreateTemporaryGeneratedAssetsRoot();

            try
            {
                Gen5WorldImportArtifactSet artifacts = Gen5WorldImportRunner.ImportFromRoot(root, generatedAssetsRoot);
                Gen5WorldDatabaseAsset worldDatabase = AssetDatabase.LoadAssetAtPath<Gen5WorldDatabaseAsset>(artifacts.WorldDatabaseAssetPath);
                GameContentProfile profile = AssetDatabase.LoadAssetAtPath<GameContentProfile>(artifacts.ProfileAssetPath);
                ContentManifest contentManifest = AssertImportedContentManifest(
                    profile,
                    artifacts.ContentManifestAssetPath,
                    artifacts.ContentVersion,
                    expectedPresentGroups: new[] { "maps" },
                    expectedAbsentGroups: new[] { "text", "scripts" });

                Assert.That(worldDatabase, Is.Not.Null);
                Assert.That(profile, Is.Not.Null);
                Assert.That(profile.LoadWorldDatabase(), Is.SameAs(worldDatabase));
                Assert.That(artifacts.ContentManifestAssetPath, Is.EqualTo(CreateGeneratedContentManifestAssetPath(generatedAssetsRoot)));
                Assert.That(contentManifest.AvailableGroups, Is.EqualTo(new[] { "maps" }));
                Assert.That(worldDatabase.SceneCount, Is.EqualTo(427));
                Assert.That(worldDatabase.MapReferenceCount, Is.EqualTo(650));
                Assert.That(worldDatabase.MapRouteCount, Is.EqualTo(650));
                Assert.That(worldDatabase.MapSideLookupCount, Is.EqualTo(652));
                Assert.That(worldDatabase.ScriptBindingCount, Is.EqualTo(854));
                Assert.That(worldDatabase.TryGetMapReference(0, out WorldMapReferenceContract mapZero), Is.True);
                Assert.That(mapZero.LogicalMapIndex, Is.EqualTo(0));
                Assert.That(mapZero.ResolvedMapIndex, Is.EqualTo(0));
                Assert.That(mapZero.IsIdentityMapping, Is.True);
                Assert.That(worldDatabase.TryGetMapRoute(429, out WorldMapRouteContract route429), Is.True);
                Assert.That(route429.ResolvedMapIndex, Is.EqualTo(200));
                Assert.That(route429.CandidateSceneIds, Is.EqualTo(new[] { "zone-0200" }));
                Assert.That(route429.CandidateZoneIndices, Is.EqualTo(new[] { 200 }));
                Assert.That(route429.SideLookup.EntryIndex, Is.EqualTo(429));
                Assert.That(route429.SideLookup.RawWord0, Is.EqualTo(13379));
                Assert.That(route429.SideLookup.RawWord1, Is.EqualTo(120));
                Assert.That(route429.SeasonalVariants, Has.Length.EqualTo(4));
                Assert.That(route429.SeasonalVariants[0].SeasonId, Is.EqualTo("slot-0"));
                Assert.That(route429.SeasonalVariants[0].VariantTokens, Is.EqualTo(new[] { "w00:0x0001", "w18:0x0400" }));
                Assert.That(route429.SeasonalVariants[1].VariantTokens, Is.EqualTo(new[] { "w18:0x0400" }));
                Assert.That(worldDatabase.TryGetSceneForLogicalMapIndex(429, out WorldSceneContract aliasUniqueScene), Is.True);
                Assert.That(aliasUniqueScene.SceneId, Is.EqualTo("zone-0200"));
                Assert.That(worldDatabase.TryGetMapRoute(462, out WorldMapRouteContract route462), Is.True);
                Assert.That(route462.ResolvedMapIndex, Is.EqualTo(81));
                Assert.That(route462.CandidateSceneIds, Is.EqualTo(new[] { "zone-0081", "zone-0082" }));
                Assert.That(route462.CandidateZoneIndices, Is.EqualTo(new[] { 81, 82 }));
                Assert.That(route462.SideLookup.EntryIndex, Is.EqualTo(462));
                Assert.That(route462.SideLookup.RawWord0, Is.EqualTo(13379));
                Assert.That(route462.SideLookup.RawWord1, Is.EqualTo(120));
                Assert.That(route462.SeasonalVariants, Has.Length.EqualTo(4));
                Assert.That(route462.SeasonalVariants[0].VariantTokens, Is.EqualTo(new[] { "w00:0x0101" }));
                Assert.That(route462.SeasonalVariants[1].VariantTokens, Is.EqualTo(new[] { "w00:0x0100" }));
                Assert.That(worldDatabase.TryGetSceneForLogicalMapIndex(462, out _), Is.False);
                Assert.That(worldDatabase.TryGetCandidateScenesForLogicalMapIndex(462, out WorldSceneContract[] aliasCandidates), Is.True);
                Assert.That(aliasCandidates, Has.Length.EqualTo(2));
                Assert.That(aliasCandidates[0].SceneId, Is.EqualTo("zone-0081"));
                Assert.That(aliasCandidates[1].SceneId, Is.EqualTo("zone-0082"));
                Assert.That(worldDatabase.TryGetMapRoute(519, out WorldMapRouteContract route519), Is.True);
                Assert.That(route519.SideLookup.EntryIndex, Is.EqualTo(519));
                Assert.That(route519.SideLookup.RawWord0, Is.EqualTo(16000));
                Assert.That(route519.SideLookup.RawWord1, Is.EqualTo(127));
                Assert.That(route519.SeasonalVariants, Has.Length.EqualTo(4));
                Assert.That(route519.SeasonalVariants[2].VariantTokens, Is.EqualTo(new[] { "w10:0x0200", "w13:0x0300", "w19:0x0300", "w22:0x0300", "w23:0x0003", "w25:0x0300" }));
                Assert.That(route519.SeasonalVariants[3].VariantTokens, Is.EqualTo(new[] { "w10:0x0200", "w19:0x0300", "w25:0x0300" }));
                Assert.That(worldDatabase.TryGetMapRoute(649, out WorldMapRouteContract route649), Is.True);
                Assert.That(route649.SideLookup.EntryIndex, Is.EqualTo(649));
                Assert.That(route649.SideLookup.RawWord0, Is.EqualTo(16000));
                Assert.That(route649.SideLookup.RawWord1, Is.EqualTo(120));
                Assert.That(route649.SeasonalVariants, Is.Empty);
                Assert.That(worldDatabase.TryGetMapSideLookup(650, out WorldMapSideLookupContract sideLookup650), Is.True);
                Assert.That(sideLookup650.RawWord0, Is.EqualTo(16000));
                Assert.That(sideLookup650.RawWord1, Is.EqualTo(120));
                Assert.That(worldDatabase.TryGetMapSideLookup(651, out WorldMapSideLookupContract sideLookup651), Is.True);
                Assert.That(sideLookup651.RawWord0, Is.EqualTo(16000));
                Assert.That(sideLookup651.RawWord1, Is.EqualTo(120));
                Assert.That(worldDatabase.TryGetScene("zone-0000", out WorldSceneContract zoneZero), Is.True);
                Assert.That(zoneZero.ZoneIndex, Is.EqualTo(0));
                Assert.That(zoneZero.SourceId, Is.EqualTo("zone-headers:0"));
                Assert.That(zoneZero.PrimaryScriptMemberIndex, Is.EqualTo(0));
                Assert.That(zoneZero.SecondaryScriptMemberIndex, Is.EqualTo(1));
                Assert.That(zoneZero.EventTextArchiveId, Is.EqualTo("event-text"));
                Assert.That(zoneZero.EventTextBankIndex, Is.EqualTo(2));
                Assert.That(zoneZero.MapReference, Is.Not.Null);
                Assert.That(zoneZero.MapReference.LogicalMapIndex, Is.EqualTo(0));
                Assert.That(zoneZero.MapReference.ResolvedMapIndex, Is.EqualTo(0));
                Assert.That(zoneZero.MapReference.IsIdentityMapping, Is.True);
                Assert.That(zoneZero.PermissionGrid.GridId, Is.EqualTo("zone-0000:permission-grid:wb:s1:p1:t4"));
                Assert.That(zoneZero.PermissionGrid.Width, Is.EqualTo(32));
                Assert.That(zoneZero.PermissionGrid.Height, Is.EqualTo(32));
                Assert.That(zoneZero.PermissionGrid.CellTokens, Has.Length.EqualTo(1024));
                Assert.That(zoneZero.PermissionGrid.CellTokens[0], Is.EqualTo("0000000001008100"));
                Assert.That(zoneZero.PermissionGrid.CellTokens[1023], Is.EqualTo("0000000001008100"));
                Assert.That(zoneZero.CameraProfile.ProfileId, Is.EqualTo("zone-0000:camera:unresolved"));
                Assert.That(zoneZero.CameraProfile.CameraMode, Is.EqualTo("unresolved"));
                Assert.That(zoneZero.SeasonalVariants, Is.Empty);
                Assert.That(worldDatabase.TryGetSceneByZoneIndex(426, out WorldSceneContract zoneLast), Is.True);
                Assert.That(zoneLast.SceneId, Is.EqualTo("zone-0426"));
                Assert.That(zoneLast.PrimaryScriptMemberIndex, Is.EqualTo(852));
                Assert.That(zoneLast.SecondaryScriptMemberIndex, Is.EqualTo(853));
                Assert.That(zoneLast.EventTextBankIndex, Is.EqualTo(468));
                Assert.That(zoneLast.MapReference.LogicalMapIndex, Is.EqualTo(426));
                Assert.That(zoneLast.MapReference.ResolvedMapIndex, Is.EqualTo(425));
                Assert.That(zoneLast.MapReference.IsIdentityMapping, Is.False);
                Assert.That(zoneLast.PermissionGrid.GridId, Is.EqualTo("zone-0426:permission-grid:unresolved"));
                Assert.That(zoneLast.PermissionGrid.CellTokens, Is.Empty);
                Assert.That(artifacts.MapReferenceCount, Is.EqualTo(650));
                Assert.That(artifacts.MapSideLookupCount, Is.EqualTo(652));
                Assert.That(artifacts.FormatSummary(), Does.Contain("Imported 427 world scenes, 652 map side lookups, and 854 script bindings"));
            }
            finally
            {
                DeleteAssetTree(generatedAssetsRoot);
            }
        }

        [Test]
        public void WorldImportRunner_Rejects_Missing_Maps_Group()
        {
            string root = CreateTemporaryImportSessionRoot(
                romInfoJson: CreateRomInfoJson(),
                sourceCatalogJson: CreateSourceCatalogJson(Array.Empty<string>()));
            string generatedAssetsRoot = CreateTemporaryGeneratedAssetsRoot();

            try
            {
                Assert.Throws<InvalidOperationException>(() => Gen5WorldImportRunner.ImportFromRoot(root, generatedAssetsRoot));
            }
            finally
            {
                DeleteAssetTree(generatedAssetsRoot);
            }
        }

        [Test]
        public void WorldImportRunner_Rejects_Missing_MapLookup_Container()
        {
            string canonicalRoot = Gen5ImportProfile.CanonicalExportRoot;
            string romInfoJson = File.ReadAllText(Path.Combine(canonicalRoot, Gen5ImportProfile.RomInfoRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            string sourceCatalogJson = File.ReadAllText(Path.Combine(canonicalRoot, Gen5ImportProfile.SourceCatalogRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            string mapsIndexJson = File.ReadAllText(Path.Combine(canonicalRoot, Gen5ImportProfile.GetGroupIndexRelativePath("maps").Replace('/', Path.DirectorySeparatorChar)));
            string root = CreateTemporaryImportSessionRoot(
                romInfoJson: romInfoJson,
                sourceCatalogJson: sourceCatalogJson,
                groupOutputs: new Dictionary<string, string>
                {
                    {
                        Gen5ImportProfile.GetGroupIndexRelativePath("maps"),
                        mapsIndexJson.Replace("\"id\": \"map-lookup\"", "\"id\": \"broken-map-lookup\"")
                    },
                });
            string generatedAssetsRoot = CreateTemporaryGeneratedAssetsRoot();

            try
            {
                Assert.Throws<InvalidDataException>(() => Gen5WorldImportRunner.ImportFromRoot(root, generatedAssetsRoot));
            }
            finally
            {
                DeleteAssetTree(generatedAssetsRoot);
            }
        }

        [Test]
        public void WorldImportRunner_Rejects_Missing_MapSideLookup_Container()
        {
            string canonicalRoot = Gen5ImportProfile.CanonicalExportRoot;
            string romInfoJson = File.ReadAllText(Path.Combine(canonicalRoot, Gen5ImportProfile.RomInfoRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            string sourceCatalogJson = File.ReadAllText(Path.Combine(canonicalRoot, Gen5ImportProfile.SourceCatalogRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            string mapsIndexJson = File.ReadAllText(Path.Combine(canonicalRoot, Gen5ImportProfile.GetGroupIndexRelativePath("maps").Replace('/', Path.DirectorySeparatorChar)));
            string root = CreateTemporaryImportSessionRoot(
                romInfoJson: romInfoJson,
                sourceCatalogJson: sourceCatalogJson,
                groupOutputs: new Dictionary<string, string>
                {
                    {
                        Gen5ImportProfile.GetGroupIndexRelativePath("maps"),
                        mapsIndexJson.Replace("\"id\": \"map-side-lookup-candidate\"", "\"id\": \"broken-map-side-lookup\"")
                    },
                });
            string generatedAssetsRoot = CreateTemporaryGeneratedAssetsRoot();

            try
            {
                Assert.Throws<InvalidDataException>(() => Gen5WorldImportRunner.ImportFromRoot(root, generatedAssetsRoot));
            }
            finally
            {
                DeleteAssetTree(generatedAssetsRoot);
            }
        }

        [Test]
        public void ScriptImportRunner_Rejects_Missing_Scripts_Group()
        {
            string root = CreateTemporaryImportSessionRoot(
                romInfoJson: CreateRomInfoJson(),
                sourceCatalogJson: CreateSourceCatalogJson(Array.Empty<string>()));
            string generatedAssetsRoot = CreateTemporaryGeneratedAssetsRoot();

            try
            {
                Assert.Throws<InvalidOperationException>(() => Gen5ScriptImportRunner.ImportFromRoot(root, generatedAssetsRoot));
            }
            finally
            {
                DeleteAssetTree(generatedAssetsRoot);
            }
        }

        [Test]
        public void TextDatabase_Resolves_Script_Text_Reference_And_Formats_Pages()
        {
            string root = CreateCanonicalTextImportSessionRoot();
            string generatedAssetsRoot = CreateTemporaryGeneratedAssetsRoot();

            try
            {
                Gen5TextImportArtifactSet artifacts = Gen5TextImportRunner.ImportFromRoot(root, generatedAssetsRoot);
                Gen5TextDatabaseAsset textDatabase = AssetDatabase.LoadAssetAtPath<Gen5TextDatabaseAsset>(artifacts.TextDatabaseAssetPath);
                ScriptTextReferenceContract reference = new ScriptTextReferenceContract
                {
                    ArchiveId = "system-text",
                    BankIndex = 3,
                    MessageIndex = 56,
                };

                Assert.That(textDatabase, Is.Not.Null);
                Assert.That(textDatabase.TryGetMessage(reference, out TextMessageContract message), Is.True);

                TestVariableResolver resolver = new TestVariableResolver();
                resolver.Add(256, new[] { 0 }, "PLAYER");
                string[] pages = Gen5TextFormatter.SplitIntoPages(message, resolver);

                Assert.That(pages, Has.Length.EqualTo(2));
                Assert.That(pages[0], Is.EqualTo("Juniper's words echoed...\nPLAYER! There's a time and"));
                Assert.That(pages[1], Is.EqualTo("\nplace for everything! But not now."));
                Assert.That(
                    Gen5TextFormatter.FormatForDisplay(message, resolver),
                    Is.EqualTo("Juniper's words echoed...\nPLAYER! There's a time and\f\nplace for everything! But not now."));
            }
            finally
            {
                DeleteAssetTree(generatedAssetsRoot);
            }
        }

        [Test]
        public void TextDatabase_Rejects_Invalid_Script_Text_Reference()
        {
            string root = CreateCanonicalTextImportSessionRoot();
            string generatedAssetsRoot = CreateTemporaryGeneratedAssetsRoot();

            try
            {
                Gen5TextImportArtifactSet artifacts = Gen5TextImportRunner.ImportFromRoot(root, generatedAssetsRoot);
                Gen5TextDatabaseAsset textDatabase = AssetDatabase.LoadAssetAtPath<Gen5TextDatabaseAsset>(artifacts.TextDatabaseAssetPath);

                Assert.That(textDatabase, Is.Not.Null);
                Assert.That(
                    textDatabase.TryGetMessage(
                        new ScriptTextReferenceContract
                        {
                            ArchiveId = "system-text",
                            BankIndex = -1,
                            MessageIndex = 56,
                        },
                        out _),
                    Is.False);
                Assert.That(
                    textDatabase.TryGetMessage(
                        new ScriptTextReferenceContract
                        {
                            ArchiveId = "missing-archive",
                            BankIndex = 0,
                            MessageIndex = 0,
                        },
                        out _),
                    Is.False);
            }
            finally
            {
                DeleteAssetTree(generatedAssetsRoot);
            }
        }

        private static string CreateTemporaryExportRoot()
        {
            string root = Path.Combine("Temp", "BlackWhiteFoundationTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "raw"));
            Directory.CreateDirectory(Path.Combine(root, "normalized"));
            Directory.CreateDirectory(Path.Combine(root, "normalized", "metadata"));
            Directory.CreateDirectory(Path.Combine(root, "manifests"));
            Directory.CreateDirectory(Path.Combine(root, "logs"));
            return root;
        }

        private static string CreateTemporaryGeneratedAssetsRoot()
        {
            return $"Assets/Generated/TestImports/{Guid.NewGuid():N}";
        }

        private static string CreateCanonicalTextImportSessionRoot()
        {
            string canonicalRoot = Gen5ImportProfile.CanonicalExportRoot;
            string romInfoJson = File.ReadAllText(Path.Combine(canonicalRoot, Gen5ImportProfile.RomInfoRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            string sourceCatalogJson = File.ReadAllText(Path.Combine(canonicalRoot, Gen5ImportProfile.SourceCatalogRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            string textIndexJson = File.ReadAllText(Path.Combine(canonicalRoot, Gen5ImportProfile.GetGroupIndexRelativePath("text").Replace('/', Path.DirectorySeparatorChar)));

            return CreateTemporaryImportSessionRoot(
                romInfoJson: romInfoJson,
                sourceCatalogJson: sourceCatalogJson,
                groupOutputs: new Dictionary<string, string>
                {
                    { Gen5ImportProfile.GetGroupIndexRelativePath("text"), textIndexJson },
                });
        }

        private static string CreateCanonicalScriptImportSessionRoot()
        {
            string canonicalRoot = Gen5ImportProfile.CanonicalExportRoot;
            string romInfoJson = File.ReadAllText(Path.Combine(canonicalRoot, Gen5ImportProfile.RomInfoRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            string sourceCatalogJson = File.ReadAllText(Path.Combine(canonicalRoot, Gen5ImportProfile.SourceCatalogRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            string mapsIndexJson = File.ReadAllText(Path.Combine(canonicalRoot, Gen5ImportProfile.GetGroupIndexRelativePath("maps").Replace('/', Path.DirectorySeparatorChar)));
            string textIndexJson = File.ReadAllText(Path.Combine(canonicalRoot, Gen5ImportProfile.GetGroupIndexRelativePath("text").Replace('/', Path.DirectorySeparatorChar)));
            string scriptsIndexJson = File.ReadAllText(Path.Combine(canonicalRoot, Gen5ImportProfile.GetGroupIndexRelativePath("scripts").Replace('/', Path.DirectorySeparatorChar)));

            return CreateTemporaryImportSessionRoot(
                romInfoJson: romInfoJson,
                sourceCatalogJson: sourceCatalogJson,
                groupOutputs: new Dictionary<string, string>
                {
                    { Gen5ImportProfile.GetGroupIndexRelativePath("maps"), mapsIndexJson },
                    { Gen5ImportProfile.GetGroupIndexRelativePath("text"), textIndexJson },
                    { Gen5ImportProfile.GetGroupIndexRelativePath("scripts"), scriptsIndexJson },
                });
        }

        private static string CreateCanonicalWorldImportSessionRoot()
        {
            string canonicalRoot = Gen5ImportProfile.CanonicalExportRoot;
            string romInfoJson = File.ReadAllText(Path.Combine(canonicalRoot, Gen5ImportProfile.RomInfoRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            string sourceCatalogJson = File.ReadAllText(Path.Combine(canonicalRoot, Gen5ImportProfile.SourceCatalogRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            string mapsIndexJson = File.ReadAllText(Path.Combine(canonicalRoot, Gen5ImportProfile.GetGroupIndexRelativePath("maps").Replace('/', Path.DirectorySeparatorChar)));

            return CreateTemporaryImportSessionRoot(
                romInfoJson: romInfoJson,
                sourceCatalogJson: sourceCatalogJson,
                groupOutputs: new Dictionary<string, string>
                {
                    { Gen5ImportProfile.GetGroupIndexRelativePath("maps"), mapsIndexJson },
                });
        }

        private static void DeleteAssetTree(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            if (AssetDatabase.DeleteAsset(assetPath))
            {
                DeleteEmptyGeneratedFolder("Assets/Generated/TestImports");
                AssetDatabase.Refresh();
            }
        }

        private static void DeleteEmptyGeneratedFolder(string assetFolderPath)
        {
            if (!AssetDatabase.IsValidFolder(assetFolderPath))
            {
                return;
            }

            string fullPath = Path.GetFullPath(assetFolderPath);
            if (Directory.Exists(fullPath) && Directory.GetFileSystemEntries(fullPath).Length == 0)
            {
                AssetDatabase.DeleteAsset(assetFolderPath);
            }
        }

        private static ContentManifest AssertImportedContentManifest(
            GameContentProfile profile,
            string contentManifestAssetPath,
            string expectedContentVersion,
            string[] expectedPresentGroups,
            string[] expectedAbsentGroups)
        {
            ContentManifest contentManifest = AssetDatabase.LoadAssetAtPath<ContentManifest>(contentManifestAssetPath);

            Assert.That(profile, Is.Not.Null);
            Assert.That(contentManifest, Is.Not.Null);
            Assert.That(profile.HasContentManifest, Is.True);
            Assert.That(profile.LoadContentManifest(), Is.SameAs(contentManifest));
            Assert.That(profile.Manifest, Is.SameAs(contentManifest));
            Assert.DoesNotThrow(profile.EnsureValid);
            Assert.That(contentManifest.SchemaVersion, Is.EqualTo(ContentSchemaVersions.ContentManifest));
            Assert.That(contentManifest.SourceSchemaVersion, Is.EqualTo(Gen5ImportProfile.SchemaVersion));
            Assert.That(contentManifest.Version, Is.Not.Null);
            Assert.That(contentManifest.ContentVersion, Is.EqualTo(expectedContentVersion));
            Assert.That(contentManifest.ContentVersion, Has.Length.EqualTo(40));
            Assert.That(contentManifest.GameId, Is.EqualTo(Gen5ImportProfile.GameId));
            Assert.That(contentManifest.ContractFamily, Is.EqualTo(profile.ContractFamily));
            Assert.That(contentManifest.ProfileId, Is.EqualTo(profile.ProfileId));
            Assert.That(contentManifest.RomFilename, Is.EqualTo("pokeblack.nds"));
            Assert.That(contentManifest.RomSha1, Is.EqualTo("a68b3bedf5c1e53556e41e59cdf396c20b331896"));
            Assert.That(contentManifest.RomSize, Is.EqualTo(268435456));
            Assert.That(contentManifest.SourceGeneratedAt, Is.Not.Empty);

            foreach (string groupName in expectedPresentGroups ?? Array.Empty<string>())
            {
                Assert.That(contentManifest.ContainsGroup(groupName), Is.True, $"Expected ContentManifest to contain group '{groupName}'.");
            }

            foreach (string groupName in expectedAbsentGroups ?? Array.Empty<string>())
            {
                Assert.That(contentManifest.ContainsGroup(groupName), Is.False, $"Expected ContentManifest to exclude group '{groupName}'.");
            }

            return contentManifest;
        }

        private static ContentManifest CreateGeneratedContentManifestAsset(string generatedAssetsRoot, ContentManifestData data)
        {
            string assetPath = CreateGeneratedContentManifestAssetPath(generatedAssetsRoot);
            EnsureAssetFolder(Path.GetDirectoryName(assetPath)?.Replace('\\', '/'));

            ContentManifest contentManifest = ScriptableObject.CreateInstance<ContentManifest>();
            contentManifest.name = "ContentManifest";
            contentManifest.Configure(data);
            AssetDatabase.CreateAsset(contentManifest, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return contentManifest;
        }

        private static string CreateGeneratedContentManifestAssetPath(string generatedAssetsRoot)
        {
            return NormalizeAssetPath(Gen5ImportProfile.CanonicalContentManifestAssetPath.Replace("Assets/Generated", NormalizeAssetPath(generatedAssetsRoot)));
        }

        private static void EnsureAssetFolder(string assetFolderPath)
        {
            if (string.IsNullOrWhiteSpace(assetFolderPath))
            {
                throw new ArgumentException("Asset folder path cannot be null or whitespace.", nameof(assetFolderPath));
            }

            string normalizedPath = NormalizeAssetPath(assetFolderPath);
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

        private static string CreateTemporaryImportSessionRoot(
            string romInfoJson,
            string sourceCatalogJson,
            Dictionary<string, string> groupOutputs = null)
        {
            string root = CreateTemporaryExportRoot();
            Dictionary<string, string> outputs = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { Gen5ImportProfile.ContractPlaceholderRelativePath, "{ }" },
                { Gen5ImportProfile.RomInfoRelativePath, romInfoJson },
                { Gen5ImportProfile.SourceCatalogRelativePath, sourceCatalogJson },
            };

            if (groupOutputs != null)
            {
                foreach (KeyValuePair<string, string> pair in groupOutputs)
                {
                    outputs[pair.Key] = pair.Value;
                }
            }

            List<string> manifestOutputPaths = new List<string>();
            foreach (KeyValuePair<string, string> pair in outputs)
            {
                string filePath = Path.Combine(root, pair.Key.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, pair.Value);
                manifestOutputPaths.Add(filePath.Replace('\\', '/'));
            }

            File.WriteAllText(
                Path.Combine(root, "manifests", "manifest.json"),
                CreateManifestJsonForOutputs(root.Replace('\\', '/'), manifestOutputPaths));

            return root;
        }

        private static string CreateRomInfoJson()
        {
            return
                "{" +
                "\"fileCount\":0,\"filename\":\"pokeblack.nds\",\"game\":\"Pokemon Black Workspace Baseline\",\"namedFileCount\":0," +
                "\"sha1\":\"a68b3bedf5c1e53556e41e59cdf396c20b331896\",\"size\":268435456,\"unnamedFileCount\":0" +
                "}";
        }

        private static string CreateSourceCatalogJson(IReadOnlyList<string> sourceEntries)
        {
            StringBuilder entriesBuilder = new StringBuilder();
            for (int index = 0; index < sourceEntries.Count; index++)
            {
                if (index > 0)
                {
                    entriesBuilder.Append(',');
                }

                entriesBuilder.Append(sourceEntries[index]);
            }

            return
                "{" +
                "\"rom\":{\"filename\":\"pokeblack.nds\",\"game\":\"Pokemon Black Workspace Baseline\",\"sha1\":\"a68b3bedf5c1e53556e41e59cdf396c20b331896\",\"size\":268435456}," +
                $"\"sourceCount\":{sourceEntries.Count},\"sources\":[{entriesBuilder}]" +
                "}";
        }

        private static string CreateSourceCatalogEntryJson(string groupName, string id, string sourcePath)
        {
            return
                "{" +
                "\"fileId\":1," +
                $"\"group\":\"{EscapeJson(groupName)}\"," +
                $"\"id\":\"{EscapeJson(id)}\"," +
                "\"largestMemberSize\":1," +
                "\"memberCount\":1," +
                "\"sha1\":\"entry-sha1\"," +
                "\"size\":1," +
                $"\"sourcePath\":\"{EscapeJson(sourcePath)}\"" +
                "}";
        }

        private static string CreateGroupIndexJson(string groupName, int containerCount)
        {
            return
                "{" +
                $"\"containerCount\":{containerCount},\"containers\":[],\"group\":\"{EscapeJson(groupName)}\"" +
                "}";
        }

        private static string CreateDecodedTextGroupIndexJson()
        {
            return
                "{" +
                "\"containerCount\":1," +
                "\"containers\":[" +
                "{" +
                "\"containerType\":\"narc\"," +
                "\"decodedMessageCount\":0," +
                "\"fileId\":1," +
                "\"id\":\"system-text\"," +
                "\"largestMemberSize\":1," +
                "\"memberCount\":1," +
                "\"members\":[" +
                "{" +
                "\"blockCount\":1," +
                "\"index\":0," +
                "\"messageCount\":1," +
                "\"messages\":[]," +
                "\"messagesPerBlock\":1," +
                "\"sha1\":\"member-sha1\"," +
                "\"size\":1" +
                "}" +
                "]," +
                "\"rawOutputPath\":\"External/Exports/BlackWhite/M0/raw/narc/a/0/0/2\"," +
                "\"sha1\":\"entry-sha1\"," +
                "\"size\":1," +
                "\"sourcePath\":\"a/0/0/2\"," +
                "\"totalMemberBytes\":1" +
                "}" +
                "]," +
                "\"group\":\"text\"," +
                "\"totalDecodedMessages\":0" +
                "}";
        }

        private static string CreateDecodedTextGroupIndexJsonWithTokenMismatch()
        {
            return
                "{" +
                "\"containerCount\":1," +
                "\"containers\":[" +
                "{" +
                "\"containerType\":\"narc\"," +
                "\"decodedMessageCount\":1," +
                "\"fileId\":1," +
                "\"id\":\"system-text\"," +
                "\"largestMemberSize\":1," +
                "\"memberCount\":1," +
                "\"members\":[" +
                "{" +
                "\"blockCount\":1," +
                "\"index\":0," +
                "\"messageCount\":1," +
                "\"messages\":[" +
                "{" +
                "\"blockIndex\":0," +
                "\"charCount\":1," +
                "\"entryIndex\":0," +
                "\"flags\":74," +
                "\"isCompressed\":false," +
                "\"text\":\"Mismatch\"," +
                "\"tokens\":[" +
                "{" +
                "\"kind\":\"text\"," +
                "\"text\":\"Other\"" +
                "}" +
                "]" +
                "}" +
                "]," +
                "\"messagesPerBlock\":1," +
                "\"sha1\":\"member-sha1\"," +
                "\"size\":1" +
                "}" +
                "]," +
                "\"rawOutputPath\":\"External/Exports/BlackWhite/M0/raw/narc/a/0/0/2\"," +
                "\"sha1\":\"entry-sha1\"," +
                "\"size\":1," +
                "\"sourcePath\":\"a/0/0/2\"," +
                "\"totalMemberBytes\":1" +
                "}" +
                "]," +
                "\"group\":\"text\"," +
                "\"totalDecodedMessages\":1" +
                "}";
        }

        private static string CreateManifestJson(string exportRoot, string outputPath, string outputHash)
        {
            return
                "{" +
                $"\"schemaVersion\":1,\"game\":\"pokemon-black\",\"rom\":{{\"filename\":\"pokeblack.nds\",\"sha1\":\"a68b3bedf5c1e53556e41e59cdf396c20b331896\",\"size\":268435456}}," +
                $"\"exportRoot\":\"{exportRoot}\",\"generatedAt\":\"2026-04-18T00:00:00Z\"," +
                $"\"normalizedOutputs\":[{{\"path\":\"{outputPath}\",\"hash\":\"{outputHash}\"}}]," +
                $"\"hashes\":{{\"{outputPath}\":\"{outputHash}\"}}" +
                "}";
        }

        private static string CreateManifestJsonForOutputs(string exportRoot, IReadOnlyList<string> outputPaths)
        {
            StringBuilder normalizedOutputs = new StringBuilder();
            StringBuilder hashes = new StringBuilder();

            for (int index = 0; index < outputPaths.Count; index++)
            {
                string outputPath = outputPaths[index];
                string outputHash = ComputeSha1(outputPath);
                if (index > 0)
                {
                    normalizedOutputs.Append(',');
                    hashes.Append(',');
                }

                normalizedOutputs.Append("{\"path\":\"");
                normalizedOutputs.Append(EscapeJson(outputPath));
                normalizedOutputs.Append("\",\"hash\":\"");
                normalizedOutputs.Append(outputHash);
                normalizedOutputs.Append("\"}");

                hashes.Append("\"");
                hashes.Append(EscapeJson(outputPath));
                hashes.Append("\":\"");
                hashes.Append(outputHash);
                hashes.Append("\"");
            }

            return
                "{" +
                $"\"schemaVersion\":1,\"game\":\"pokemon-black\",\"rom\":{{\"filename\":\"pokeblack.nds\",\"sha1\":\"a68b3bedf5c1e53556e41e59cdf396c20b331896\",\"size\":268435456}}," +
                $"\"exportRoot\":\"{EscapeJson(exportRoot)}\",\"generatedAt\":\"2026-04-18T00:00:00Z\"," +
                $"\"normalizedOutputs\":[{normalizedOutputs}],\"hashes\":{{{hashes}}}" +
                "}";
        }

        private static string CreateValidationReportJson(string rootPath)
        {
            return
                "{" +
                $"\"rootPath\":\"{EscapeJson(rootPath)}\"," +
                "\"game\":\"pokemon-black\"," +
                "\"romFilename\":\"pokeblack.nds\"," +
                "\"romSha1\":\"a68b3bedf5c1e53556e41e59cdf396c20b331896\"," +
                "\"romSize\":268435456," +
                "\"sourceCount\":0," +
                "\"groupSummaries\":[]" +
                "}";
        }

        private static string ComputeSha1(string path)
        {
            using SHA1 sha1 = SHA1.Create();
            using FileStream stream = File.OpenRead(path);
            byte[] hash = sha1.ComputeHash(stream);
            StringBuilder builder = new StringBuilder(hash.Length * 2);
            foreach (byte value in hash)
            {
                builder.Append(value.ToString("x2"));
            }

            return builder.ToString();
        }

        private static string EscapeJson(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private sealed class TestVariableResolver : IGen5TextVariableResolver
        {
            private readonly Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);

            public void Add(int controlCode, int[] arguments, string value)
            {
                values[BuildKey(controlCode, arguments)] = value;
            }

            public bool TryResolve(int controlCode, int[] arguments, out string value)
            {
                return values.TryGetValue(BuildKey(controlCode, arguments), out value);
            }

            private static string BuildKey(int controlCode, int[] arguments)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(controlCode);
                builder.Append(':');
                if (arguments != null)
                {
                    for (int index = 0; index < arguments.Length; index++)
                    {
                        if (index > 0)
                        {
                            builder.Append(',');
                        }

                        builder.Append(arguments[index]);
                    }
                }

                return builder.ToString();
            }
        }
    }
}
