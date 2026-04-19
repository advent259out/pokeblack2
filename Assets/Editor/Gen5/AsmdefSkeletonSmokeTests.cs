using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace PokeBlack2.Foundation.Editor
{
    public sealed class AsmdefSkeletonSmokeTests
    {
        [Test]
        public void PlannedAsmdefSkeleton_Matches_ArchitectureDraft()
        {
            AsmdefExpectation[] expectations =
            {
                CreateExpectation(
                    "Assets/PokeBlack/Bootstrap/Runtime/PokeBlack.Bootstrap.asmdef",
                    "PokeBlack.Bootstrap",
                    "PokeBlack.Bootstrap",
                    new[]
                    {
                        "PokeBlack.Core",
                        "PokeBlack.Content.Runtime",
                        "PokeBlack.Infrastructure",
                        "PokeBlack.World.Runtime",
                        "PokeBlack.Battle.Runtime",
                        "PokeBlack.UI.Runtime",
                    }),
                CreateExpectation(
                    "Assets/PokeBlack/Bootstrap/Tests/PlayMode/PokeBlack.Bootstrap.Tests.PlayMode.asmdef",
                    "PokeBlack.Bootstrap.Tests.PlayMode",
                    "PokeBlack.Bootstrap.Tests.PlayMode",
                    new[] { "PokeBlack.Bootstrap" },
                    optionalUnityReferences: new[] { "TestAssemblies" }),
                CreateExpectation(
                    "Assets/PokeBlack/Core/Runtime/PokeBlack.Core.asmdef",
                    "PokeBlack.Core",
                    "PokeBlack.Core",
                    Array.Empty<string>(),
                    noEngineReferences: true),
                CreateExpectation(
                    "Assets/PokeBlack/Core/Tests/EditMode/PokeBlack.Core.Tests.EditMode.asmdef",
                    "PokeBlack.Core.Tests.EditMode",
                    "PokeBlack.Core.Tests.EditMode",
                    new[] { "PokeBlack.Core" },
                    includePlatforms: new[] { "Editor" },
                    optionalUnityReferences: new[] { "TestAssemblies" }),
                CreateExpectation(
                    "Assets/PokeBlack/Content/Contracts/PokeBlack.Content.Contracts.asmdef",
                    "PokeBlack.Content.Contracts",
                    "PokeBlack.Content.Contracts",
                    Array.Empty<string>(),
                    noEngineReferences: true),
                CreateExpectation(
                    "Assets/PokeBlack/Content/Runtime/PokeBlack.Content.Runtime.asmdef",
                    "PokeBlack.Content.Runtime",
                    "PokeBlack.Content.Runtime",
                    new[] { "PokeBlack.Content.Contracts" }),
                CreateExpectation(
                    "Assets/PokeBlack/Content/Import/Editor/PokeBlack.Content.Import.Editor.asmdef",
                    "PokeBlack.Content.Import.Editor",
                    "PokeBlack.Content.Import.Editor",
                    new[]
                    {
                        "PokeBlack.Content.Contracts",
                        "PokeBlack.Content.Runtime",
                    },
                    includePlatforms: new[] { "Editor" }),
                CreateExpectation(
                    "Assets/PokeBlack/Infrastructure/Runtime/PokeBlack.Infrastructure.asmdef",
                    "PokeBlack.Infrastructure",
                    "PokeBlack.Infrastructure",
                    new[]
                    {
                        "PokeBlack.Core",
                        "PokeBlack.Content.Runtime",
                    }),
                CreateExpectation(
                    "Assets/PokeBlack/Infrastructure/Editor/PokeBlack.Infrastructure.Editor.asmdef",
                    "PokeBlack.Infrastructure.Editor",
                    "PokeBlack.Infrastructure.Editor",
                    new[] { "PokeBlack.Infrastructure" },
                    includePlatforms: new[] { "Editor" }),
                CreateExpectation(
                    "Assets/PokeBlack/World/Runtime/PokeBlack.World.Runtime.asmdef",
                    "PokeBlack.World.Runtime",
                    "PokeBlack.World.Runtime",
                    new[]
                    {
                        "PokeBlack.Core",
                        "PokeBlack.Content.Runtime",
                        "PokeBlack.Infrastructure",
                    }),
                CreateExpectation(
                    "Assets/PokeBlack/World/Tests/PlayMode/PokeBlack.World.Tests.PlayMode.asmdef",
                    "PokeBlack.World.Tests.PlayMode",
                    "PokeBlack.World.Tests.PlayMode",
                    new[] { "PokeBlack.World.Runtime" },
                    optionalUnityReferences: new[] { "TestAssemblies" }),
                CreateExpectation(
                    "Assets/PokeBlack/Battle/Runtime/PokeBlack.Battle.Runtime.asmdef",
                    "PokeBlack.Battle.Runtime",
                    "PokeBlack.Battle.Runtime",
                    new[]
                    {
                        "PokeBlack.Core",
                        "PokeBlack.Content.Runtime",
                        "PokeBlack.Infrastructure",
                    }),
                CreateExpectation(
                    "Assets/PokeBlack/Battle/Tests/PlayMode/PokeBlack.Battle.Tests.PlayMode.asmdef",
                    "PokeBlack.Battle.Tests.PlayMode",
                    "PokeBlack.Battle.Tests.PlayMode",
                    new[] { "PokeBlack.Battle.Runtime" },
                    optionalUnityReferences: new[] { "TestAssemblies" }),
                CreateExpectation(
                    "Assets/PokeBlack/UI/Runtime/PokeBlack.UI.Runtime.asmdef",
                    "PokeBlack.UI.Runtime",
                    "PokeBlack.UI.Runtime",
                    new[]
                    {
                        "PokeBlack.Core",
                        "PokeBlack.Content.Runtime",
                        "PokeBlack.Infrastructure",
                    }),
                CreateExpectation(
                    "Assets/PokeBlack/UI/Editor/PokeBlack.UI.Editor.asmdef",
                    "PokeBlack.UI.Editor",
                    "PokeBlack.UI.Editor",
                    new[] { "PokeBlack.UI.Runtime" },
                    includePlatforms: new[] { "Editor" }),
                CreateExpectation(
                    "Assets/PokeBlack/Tools/Editor/PokeBlack.Tools.Editor.asmdef",
                    "PokeBlack.Tools.Editor",
                    "PokeBlack.Tools.Editor",
                    new[] { "PokeBlack.Content.Runtime" },
                    includePlatforms: new[] { "Editor" }),
            };

            foreach (AsmdefExpectation expectation in expectations)
            {
                AssertAsmdef(expectation);
            }

            Assert.That(File.Exists("Assets/PokeBlack/Content/Generated/.gitkeep"), Is.True);
            Assert.That(File.Exists("Assets/PokeBlack/Content/AuthoredOverrides/.gitkeep"), Is.True);
        }

        private static void AssertAsmdef(AsmdefExpectation expectation)
        {
            Assert.That(File.Exists(expectation.Path), Is.True, $"Missing asmdef: {expectation.Path}");

            AsmdefData asmdef = JsonUtility.FromJson<AsmdefData>(File.ReadAllText(expectation.Path));
            Assert.That(asmdef, Is.Not.Null, $"Unable to parse asmdef JSON at {expectation.Path}");
            Assert.That(asmdef.name, Is.EqualTo(expectation.Name), expectation.Path);
            Assert.That(asmdef.rootNamespace, Is.EqualTo(expectation.RootNamespace), expectation.Path);
            Assert.That(asmdef.references ?? Array.Empty<string>(), Is.EqualTo(expectation.References), expectation.Path);
            Assert.That(asmdef.includePlatforms ?? Array.Empty<string>(), Is.EqualTo(expectation.IncludePlatforms), expectation.Path);
            Assert.That(asmdef.optionalUnityReferences ?? Array.Empty<string>(), Is.EqualTo(expectation.OptionalUnityReferences), expectation.Path);
            Assert.That(asmdef.noEngineReferences, Is.EqualTo(expectation.NoEngineReferences), expectation.Path);
        }

        private static AsmdefExpectation CreateExpectation(
            string path,
            string name,
            string rootNamespace,
            string[] references,
            string[] includePlatforms = null,
            string[] optionalUnityReferences = null,
            bool noEngineReferences = false)
        {
            return new AsmdefExpectation
            {
                Path = path,
                Name = name,
                RootNamespace = rootNamespace,
                References = references ?? Array.Empty<string>(),
                IncludePlatforms = includePlatforms ?? Array.Empty<string>(),
                OptionalUnityReferences = optionalUnityReferences ?? Array.Empty<string>(),
                NoEngineReferences = noEngineReferences,
            };
        }

        [Serializable]
        private sealed class AsmdefData
        {
            public string name;
            public string rootNamespace;
            public string[] references;
            public string[] includePlatforms;
            public string[] optionalUnityReferences;
            public bool noEngineReferences;
        }

        private sealed class AsmdefExpectation
        {
            public string Path { get; set; }

            public string Name { get; set; }

            public string RootNamespace { get; set; }

            public string[] References { get; set; }

            public string[] IncludePlatforms { get; set; }

            public string[] OptionalUnityReferences { get; set; }

            public bool NoEngineReferences { get; set; }
        }
    }
}
