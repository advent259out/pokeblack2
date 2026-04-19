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
    public static class Gen5WorldImportRunner
    {
        [MenuItem("PokeBlack2/Gen5/Import World Metadata")]
        public static void ImportCanonicalFromMenu()
        {
            Gen5WorldImportArtifactSet artifacts = ImportCanonical();
            Debug.Log(artifacts.FormatSummary());
            UnityEngine.Object importedAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(artifacts.WorldDatabaseAssetPath);
            if (importedAsset != null)
            {
                EditorGUIUtility.PingObject(importedAsset);
            }
        }

        public static Gen5WorldImportArtifactSet ImportCanonical()
        {
            return ImportFromRoot(Gen5ImportProfile.CanonicalExportRoot, Gen5ImportProfile.GeneratedAssetsRoot);
        }

        public static Gen5WorldImportArtifactSet ImportFromRoot(string rootPath, string generatedAssetsRoot)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("World import root path cannot be null or whitespace.", nameof(rootPath));
            }

            if (string.IsNullOrWhiteSpace(generatedAssetsRoot))
            {
                throw new ArgumentException("Generated assets root cannot be null or whitespace.", nameof(generatedAssetsRoot));
            }

            string normalizedGeneratedAssetsRoot = NormalizeAssetPath(generatedAssetsRoot);
            Gen5FoundationImportSession session = Gen5FoundationImportSession.LoadFromRoot(rootPath);
            if (!session.HasGroup("maps"))
            {
                throw new InvalidOperationException("The current export root does not contain a normalized 'maps' group.");
            }

            IReadOnlyList<NormalizedSourceCatalogEntry> mapSources = session.GetSourcesForGroup("maps");
            if (mapSources.Count == 0)
            {
                throw new InvalidDataException("The normalized 'maps' group is present, but no map sources were registered.");
            }

            NormalizedMapGroupIndex groupIndex = session.LoadMapGroupIndex();
            WorldImportContractSet contracts = BuildWorldContracts(mapSources, groupIndex);
            WorldSceneContract[] scenes = contracts.Scenes;
            WorldMapReferenceContract[] mapReferences = contracts.MapReferences;
            WorldMapRouteContract[] mapRoutes = contracts.MapRoutes;
            WorldMapSideLookupContract[] mapSideLookups = contracts.MapSideLookups;
            string resourcesRoot = CombineAssetPath(normalizedGeneratedAssetsRoot, "Resources");
            string worldDatabaseAssetPath = CombineAssetPath(resourcesRoot, "Imported/Gen5/World/CanonicalGen5WorldDatabase.asset");
            string profileAssetPath = CombineAssetPath(resourcesRoot, "Foundation/GameContentProfile.asset");

            EnsureAssetFolder(Path.GetDirectoryName(worldDatabaseAssetPath)?.Replace('\\', '/'));
            EnsureAssetFolder(Path.GetDirectoryName(profileAssetPath)?.Replace('\\', '/'));

            Gen5WorldDatabaseAsset worldDatabase = LoadOrCreateAsset(
                worldDatabaseAssetPath,
                () => ScriptableObject.CreateInstance<Gen5WorldDatabaseAsset>());
            worldDatabase.name = "CanonicalGen5WorldDatabase";
            worldDatabase.Configure(
                session.RootPath,
                GameVersion.PokemonBlackUsaEurope,
                session.RomInfo.Filename,
                session.RomInfo.Sha1,
                scenes,
                mapReferences,
                mapRoutes,
                mapSideLookups);
            EditorUtility.SetDirty(worldDatabase);

            GameContentProfile profile = LoadOrCreateAsset(
                profileAssetPath,
                () => ScriptableObject.CreateInstance<GameContentProfile>());
            profile.name = "GameContentProfile";
            ContentManifest contentManifest = ContentManifestImportUtility.ImportForSession(session, normalizedGeneratedAssetsRoot);
            profile.ApplyContentManifest(contentManifest);
            profile.ApplyImportedWorldDatabase(worldDatabase);
            EditorUtility.SetDirty(profile);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return new Gen5WorldImportArtifactSet
            {
                RootPath = session.RootPath,
                GeneratedAssetsRoot = normalizedGeneratedAssetsRoot,
                ProfileAssetPath = profileAssetPath,
                ContentManifestAssetPath = AssetDatabase.GetAssetPath(contentManifest),
                ContentVersion = contentManifest.ContentVersion,
                WorldDatabaseAssetPath = worldDatabaseAssetPath,
                SceneCount = scenes.Length,
                MapReferenceCount = mapReferences.Length,
                MapRouteCount = mapRoutes.Length,
                MapSideLookupCount = mapSideLookups.Length,
                ScriptBindingCount = CountScriptBindings(scenes),
            };
        }

        private static WorldImportContractSet BuildWorldContracts(
            IReadOnlyList<NormalizedSourceCatalogEntry> mapSources,
            NormalizedMapGroupIndex groupIndex)
        {
            if (groupIndex.ContainerCount != mapSources.Count)
            {
                throw new InvalidDataException(
                    $"Map group source count '{mapSources.Count}' does not match map container count '{groupIndex.ContainerCount}'.");
            }

            Dictionary<string, NormalizedSourceCatalogEntry> sourcesByKey =
                new Dictionary<string, NormalizedSourceCatalogEntry>(StringComparer.Ordinal);
            foreach (NormalizedSourceCatalogEntry source in mapSources)
            {
                sourcesByKey.Add(BuildContainerKey(source.FileId, source.Id, source.SourcePath), source);
            }

            NormalizedMapContainer mapContainer = null;
            NormalizedMapContainer zoneHeaderContainer = null;
            NormalizedMapContainer mapLookupContainer = null;
            NormalizedMapContainer mapMetadataCandidateContainer = null;
            NormalizedMapContainer mapSideLookupContainer = null;
            foreach (NormalizedMapContainer container in groupIndex.Containers)
            {
                string key = BuildContainerKey(container.FileId, container.Id, container.SourcePath);
                if (!sourcesByKey.TryGetValue(key, out NormalizedSourceCatalogEntry source))
                {
                    throw new InvalidDataException(
                        $"Map container '{container.Id}' (fileId={container.FileId}) is missing a matching source-catalog entry.");
                }

                ValidateMapContainer(source, container);
                if (string.Equals(container.Id, "map-containers", StringComparison.Ordinal))
                {
                    mapContainer = container;
                }

                if (string.Equals(container.Id, "zone-headers", StringComparison.Ordinal))
                {
                    zoneHeaderContainer = container;
                }

                if (string.Equals(container.Id, "map-lookup", StringComparison.Ordinal))
                {
                    mapLookupContainer = container;
                }

                if (string.Equals(container.Id, "map-metadata-candidate", StringComparison.Ordinal))
                {
                    mapMetadataCandidateContainer = container;
                }

                if (string.Equals(container.Id, "map-side-lookup-candidate", StringComparison.Ordinal))
                {
                    mapSideLookupContainer = container;
                }
            }

            if (mapContainer == null)
            {
                throw new InvalidDataException("The normalized 'maps' group is missing the required 'map-containers' container.");
            }

            if (zoneHeaderContainer == null)
            {
                throw new InvalidDataException("The normalized 'maps' group is missing the required 'zone-headers' container.");
            }

            if (mapLookupContainer == null)
            {
                throw new InvalidDataException("The normalized 'maps' group is missing the required 'map-lookup' container.");
            }

            if (mapMetadataCandidateContainer == null)
            {
                throw new InvalidDataException("The normalized 'maps' group is missing the required 'map-metadata-candidate' container.");
            }

            if (mapSideLookupContainer == null)
            {
                throw new InvalidDataException("The normalized 'maps' group is missing the required 'map-side-lookup-candidate' container.");
            }

            WorldMapReferenceContract[] mapReferences = BuildMapReferences(mapLookupContainer);
            Dictionary<int, NormalizedMapContainerLayout> layoutsByMapContainerIndex =
                BuildMapContainerLayoutsByMapContainerIndex(mapContainer);
            WorldSceneContract[] scenes = BuildScenes(
                zoneHeaderContainer,
                mapLookupContainer,
                layoutsByMapContainerIndex,
                mapContainer.MemberCount,
                groupIndex.TotalScriptTextBindings);
            WorldMapSideLookupContract[] mapSideLookups = BuildMapSideLookups(mapSideLookupContainer);
            return new WorldImportContractSet
            {
                Scenes = scenes,
                MapReferences = mapReferences,
                MapRoutes = BuildMapRoutes(mapReferences, scenes, mapMetadataCandidateContainer, mapSideLookups),
                MapSideLookups = mapSideLookups,
            };
        }

        private static WorldSceneContract[] BuildScenes(
            NormalizedMapContainer zoneHeaderContainer,
            NormalizedMapContainer mapLookupContainer,
            IReadOnlyDictionary<int, NormalizedMapContainerLayout> layoutsByMapContainerIndex,
            int mapContainerCount,
            int totalScriptTextBindings)
        {
            if (mapLookupContainer.LookupCount < zoneHeaderContainer.ZoneCount)
            {
                throw new InvalidDataException(
                    $"Map lookup container exposes '{mapLookupContainer.LookupCount}' logical map entries, but zone headers require at least '{zoneHeaderContainer.ZoneCount}'.");
            }

            int countedBindings = 0;
            List<WorldSceneContract> scenes = new List<WorldSceneContract>(zoneHeaderContainer.ZoneCount);
            foreach (NormalizedZoneHeader zone in zoneHeaderContainer.Zones ?? new List<NormalizedZoneHeader>())
            {
                ValidateZone(zone);
                NormalizedMapLookupEntry mapLookupEntry = ResolveSceneMapLookupEntry(
                    zone,
                    mapLookupContainer,
                    mapContainerCount);
                countedBindings += zone.ScriptTextBindingCount;
                scenes.Add(CreateScene(
                    zone,
                    mapLookupEntry,
                    ResolveMapContainerLayout(layoutsByMapContainerIndex, mapLookupEntry.ResolvedMapIndex)));
            }

            if (zoneHeaderContainer.ZoneCount != scenes.Count)
            {
                throw new InvalidDataException(
                    $"Zone header container declares '{zoneHeaderContainer.ZoneCount}' zones, but decoded zone entry count is '{scenes.Count}'.");
            }

            if (zoneHeaderContainer.ScriptTextBindingCount != countedBindings)
            {
                throw new InvalidDataException(
                    $"Zone header container declares '{zoneHeaderContainer.ScriptTextBindingCount}' script/text bindings, but decoded binding count is '{countedBindings}'.");
            }

            if (totalScriptTextBindings != countedBindings)
            {
                throw new InvalidDataException(
                    $"Map group declares '{totalScriptTextBindings}' script/text bindings, but zone-header bindings decode to '{countedBindings}'.");
            }

            return scenes.ToArray();
        }

        private static WorldMapReferenceContract[] BuildMapReferences(NormalizedMapContainer mapLookupContainer)
        {
            List<WorldMapReferenceContract> mapReferences = new List<WorldMapReferenceContract>(mapLookupContainer.LookupCount);
            foreach (NormalizedMapLookupEntry lookupEntry in mapLookupContainer.MapLookupEntries ?? new List<NormalizedMapLookupEntry>())
            {
                if (lookupEntry == null)
                {
                    continue;
                }

                mapReferences.Add(CreateMapReference(lookupEntry));
            }

            return mapReferences.ToArray();
        }

        private static WorldMapRouteContract[] BuildMapRoutes(
            IReadOnlyList<WorldMapReferenceContract> mapReferences,
            IReadOnlyList<WorldSceneContract> scenes,
            NormalizedMapContainer mapMetadataCandidateContainer,
            IReadOnlyList<WorldMapSideLookupContract> mapSideLookups)
        {
            if (mapSideLookups == null || mapSideLookups.Count < mapReferences.Count)
            {
                throw new InvalidDataException(
                    $"Map side lookup count '{(mapSideLookups == null ? 0 : mapSideLookups.Count)}' must cover all logical maps '{mapReferences.Count}'.");
            }

            Dictionary<int, List<WorldSceneContract>> scenesByResolvedMapIndex =
                new Dictionary<int, List<WorldSceneContract>>();
            foreach (WorldSceneContract scene in scenes ?? Array.Empty<WorldSceneContract>())
            {
                if (scene == null || scene.MapReference == null || scene.MapReference.ResolvedMapIndex < 0)
                {
                    continue;
                }

                if (!scenesByResolvedMapIndex.TryGetValue(scene.MapReference.ResolvedMapIndex, out List<WorldSceneContract> matchingScenes))
                {
                    matchingScenes = new List<WorldSceneContract>();
                    scenesByResolvedMapIndex.Add(scene.MapReference.ResolvedMapIndex, matchingScenes);
                }

                matchingScenes.Add(scene);
            }

            List<WorldMapRouteContract> routes = new List<WorldMapRouteContract>(mapReferences.Count);
            Dictionary<int, NormalizedMapMetadataCandidate> metadataCandidatesByLogicalMapIndex =
                BuildMetadataCandidatesByLogicalMapIndex(mapMetadataCandidateContainer);
            foreach (WorldMapReferenceContract mapReference in mapReferences ?? Array.Empty<WorldMapReferenceContract>())
            {
                if (mapReference == null)
                {
                    continue;
                }

                if (!scenesByResolvedMapIndex.TryGetValue(mapReference.ResolvedMapIndex, out List<WorldSceneContract> matchingScenes))
                {
                    matchingScenes = new List<WorldSceneContract>();
                }

                if (mapReference.LogicalMapIndex < 0 || mapReference.LogicalMapIndex >= mapSideLookups.Count)
                {
                    throw new InvalidDataException(
                        $"Logical map '{mapReference.LogicalMapIndex}' does not have a corresponding side lookup entry. Side lookup count is '{mapSideLookups.Count}'.");
                }

                WorldMapSideLookupContract sideLookup = CloneMapSideLookup(mapSideLookups[mapReference.LogicalMapIndex]);
                if (sideLookup.EntryIndex != mapReference.LogicalMapIndex)
                {
                    throw new InvalidDataException(
                        $"Logical map '{mapReference.LogicalMapIndex}' expected side lookup entry '{mapReference.LogicalMapIndex}', but decoded '{sideLookup.EntryIndex}'.");
                }

                routes.Add(new WorldMapRouteContract
                {
                    LogicalMapIndex = mapReference.LogicalMapIndex,
                    ResolvedMapIndex = mapReference.ResolvedMapIndex,
                    IsIdentityMapping = mapReference.IsIdentityMapping,
                    CandidateSceneIds = CollectCandidateSceneIds(matchingScenes),
                    CandidateZoneIndices = CollectCandidateZoneIndices(matchingScenes),
                    SideLookup = sideLookup,
                    SeasonalVariants = CreateSeasonalVariants(
                        metadataCandidatesByLogicalMapIndex.TryGetValue(mapReference.LogicalMapIndex, out NormalizedMapMetadataCandidate candidate)
                            ? candidate
                            : null),
                });
            }

            return routes.ToArray();
        }

        private static void ValidateMapContainer(NormalizedSourceCatalogEntry source, NormalizedMapContainer container)
        {
            if (source.MemberCount != container.MemberCount)
            {
                throw new InvalidDataException(
                    $"Map container '{container.Id}' member count '{container.MemberCount}' does not match source-catalog member count '{source.MemberCount}'.");
            }

            if (source.Size != container.Size)
            {
                throw new InvalidDataException(
                    $"Map container '{container.Id}' size '{container.Size}' does not match source-catalog size '{source.Size}'.");
            }

            if (!string.Equals(source.Sha1, container.Sha1, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Map container '{container.Id}' sha1 '{container.Sha1}' does not match source-catalog sha1 '{source.Sha1}'.");
            }

            if (container.Members == null)
            {
                throw new InvalidDataException($"Map container '{container.Id}' members list is required.");
            }

            if (container.MemberCount != container.Members.Count)
            {
                throw new InvalidDataException(
                    $"Map container '{container.Id}' member count '{container.MemberCount}' does not match the decoded member entry count '{container.Members.Count}'.");
            }

            if (string.Equals(container.Id, "zone-headers", StringComparison.Ordinal))
            {
                if (container.Zones == null)
                {
                    throw new InvalidDataException("Zone header container requires a zones list.");
                }

                if (container.ScriptTextBindings == null)
                {
                    throw new InvalidDataException("Zone header container requires a scriptTextBindings list.");
                }
            }

            if (string.Equals(container.Id, "map-containers", StringComparison.Ordinal))
            {
                if (container.MapContainerLayouts == null)
                {
                    throw new InvalidDataException("Map container requires a mapContainerLayouts list.");
                }

                if (container.MapContainerLayoutCount != container.MapContainerLayouts.Count)
                {
                    throw new InvalidDataException(
                        $"Map container declares '{container.MapContainerLayoutCount}' layouts, but decoded layout entry count is '{container.MapContainerLayouts.Count}'.");
                }
            }

            if (string.Equals(container.Id, "map-lookup", StringComparison.Ordinal) &&
                container.MapLookupEntries == null)
            {
                throw new InvalidDataException("Map lookup container requires a mapLookupEntries list.");
            }

            if (string.Equals(container.Id, "map-metadata-candidate", StringComparison.Ordinal))
            {
                if (container.MapMetadataCandidates == null)
                {
                    throw new InvalidDataException("Map metadata candidate container requires a mapMetadataCandidates list.");
                }

                if (container.CandidateCount != container.MapMetadataCandidates.Count)
                {
                    throw new InvalidDataException(
                        $"Map metadata candidate container declares '{container.CandidateCount}' candidates, but decoded candidate entry count is '{container.MapMetadataCandidates.Count}'.");
                }
            }

            if (string.Equals(container.Id, "map-side-lookup-candidate", StringComparison.Ordinal))
            {
                if (container.MapSideLookupEntries == null)
                {
                    throw new InvalidDataException("Map side lookup container requires a mapSideLookupEntries list.");
                }

                if (container.SideLookupEntryCount != container.MapSideLookupEntries.Count)
                {
                    throw new InvalidDataException(
                        $"Map side lookup container declares '{container.SideLookupEntryCount}' entries, but decoded entry count is '{container.MapSideLookupEntries.Count}'.");
                }
            }
        }

        private static void ValidateZone(NormalizedZoneHeader zone)
        {
            if (zone == null)
            {
                throw new InvalidDataException("Zone header entries are required.");
            }

            if (zone.ZoneIndex < 0)
            {
                throw new InvalidDataException($"Zone header entries require a non-negative zone index, but decoded '{zone.ZoneIndex}'.");
            }

            if (zone.PrimaryScriptMemberIndex < 0 || zone.SecondaryScriptMemberIndex < 0)
            {
                throw new InvalidDataException(
                    $"Zone '{zone.ZoneIndex}' requires non-negative primary and secondary script member indices, but decoded '{zone.PrimaryScriptMemberIndex}' and '{zone.SecondaryScriptMemberIndex}'.");
            }

            if (string.IsNullOrWhiteSpace(zone.EventTextArchiveId))
            {
                throw new InvalidDataException($"Zone '{zone.ZoneIndex}' requires a non-empty event text archive id.");
            }

            if (zone.EventTextBankIndex < 0)
            {
                throw new InvalidDataException(
                    $"Zone '{zone.ZoneIndex}' requires a non-negative event text bank index, but decoded '{zone.EventTextBankIndex}'.");
            }

            int bindingCount = zone.ScriptTextBindings == null ? 0 : zone.ScriptTextBindings.Count;
            if (zone.ScriptTextBindingCount != bindingCount)
            {
                throw new InvalidDataException(
                    $"Zone '{zone.ZoneIndex}' declares '{zone.ScriptTextBindingCount}' script/text bindings, but decoded binding entry count is '{bindingCount}'.");
            }

            if (bindingCount != 2)
            {
                throw new InvalidDataException(
                    $"Zone '{zone.ZoneIndex}' must resolve exactly two script/text bindings in this foundation phase, but decoded '{bindingCount}'.");
            }

            ValidateZoneBinding(zone, zone.ScriptTextBindings[0], zone.PrimaryScriptMemberIndex);
            ValidateZoneBinding(zone, zone.ScriptTextBindings[1], zone.SecondaryScriptMemberIndex);
        }

        private static void ValidateZoneBinding(NormalizedZoneHeader zone, NormalizedMapScriptTextBinding binding, int expectedScriptMemberIndex)
        {
            if (binding == null)
            {
                throw new InvalidDataException($"Zone '{zone.ZoneIndex}' contains a null script/text binding.");
            }

            if (binding.ZoneIndex != zone.ZoneIndex)
            {
                throw new InvalidDataException(
                    $"Zone '{zone.ZoneIndex}' contains a script/text binding with mismatched zone index '{binding.ZoneIndex}'.");
            }

            if (binding.ScriptMemberIndex != expectedScriptMemberIndex)
            {
                throw new InvalidDataException(
                    $"Zone '{zone.ZoneIndex}' expected script member '{expectedScriptMemberIndex}', but decoded '{binding.ScriptMemberIndex}'.");
            }

            if (!string.Equals(binding.TextArchiveId, zone.EventTextArchiveId, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Zone '{zone.ZoneIndex}' binding archive '{binding.TextArchiveId}' does not match event text archive '{zone.EventTextArchiveId}'.");
            }

            if (binding.TextBankIndex != zone.EventTextBankIndex)
            {
                throw new InvalidDataException(
                    $"Zone '{zone.ZoneIndex}' binding bank '{binding.TextBankIndex}' does not match event text bank '{zone.EventTextBankIndex}'.");
            }
        }

        private static NormalizedMapLookupEntry ResolveSceneMapLookupEntry(
            NormalizedZoneHeader zone,
            NormalizedMapContainer mapLookupContainer,
            int mapContainerCount)
        {
            if (zone.ZoneIndex >= mapLookupContainer.MapLookupEntries.Count)
            {
                throw new InvalidDataException(
                    $"Zone '{zone.ZoneIndex}' does not have a matching map lookup entry. Map lookup count is '{mapLookupContainer.MapLookupEntries.Count}'.");
            }

            NormalizedMapLookupEntry lookupEntry = mapLookupContainer.MapLookupEntries[zone.ZoneIndex];
            if (lookupEntry == null)
            {
                throw new InvalidDataException($"Zone '{zone.ZoneIndex}' resolved to a null map lookup entry.");
            }

            if (lookupEntry.LogicalMapIndex != zone.ZoneIndex)
            {
                throw new InvalidDataException(
                    $"Zone '{zone.ZoneIndex}' expected map lookup logical index '{zone.ZoneIndex}', but decoded '{lookupEntry.LogicalMapIndex}'.");
            }

            if (lookupEntry.ResolvedMapIndex >= mapContainerCount)
            {
                throw new InvalidDataException(
                    $"Zone '{zone.ZoneIndex}' resolved map index '{lookupEntry.ResolvedMapIndex}' exceeds available map container member count '{mapContainerCount}'.");
            }

            return lookupEntry;
        }

        private static WorldSceneContract CreateScene(NormalizedZoneHeader zone, NormalizedMapLookupEntry mapLookupEntry)
        {
            return CreateScene(zone, mapLookupEntry, null);
        }

        private static WorldSceneContract CreateScene(
            NormalizedZoneHeader zone,
            NormalizedMapLookupEntry mapLookupEntry,
            NormalizedMapContainerLayout mapContainerLayout)
        {
            string zoneToken = $"zone-{zone.ZoneIndex:D4}";
            return new WorldSceneContract
            {
                SceneId = zoneToken,
                SourceId = $"zone-headers:{zone.ZoneIndex}",
                ZoneIndex = zone.ZoneIndex,
                PrimaryScriptMemberIndex = zone.PrimaryScriptMemberIndex,
                SecondaryScriptMemberIndex = zone.SecondaryScriptMemberIndex,
                EventTextArchiveId = zone.EventTextArchiveId ?? string.Empty,
                EventTextBankIndex = zone.EventTextBankIndex,
                MapReference = CreateMapReference(mapLookupEntry),
                PermissionGrid = CreatePermissionGrid(zoneToken, mapContainerLayout),
                CameraProfile = new CameraProfileContract
                {
                    ProfileId = $"{zoneToken}:camera:unresolved",
                    CameraMode = "unresolved",
                    DefaultDistance = 0f,
                    DefaultPitch = 0f,
                },
                SeasonalVariants = Array.Empty<SeasonProfileContract>(),
            };
        }

        private static Dictionary<int, NormalizedMapContainerLayout> BuildMapContainerLayoutsByMapContainerIndex(
            NormalizedMapContainer mapContainer)
        {
            Dictionary<int, NormalizedMapContainerLayout> layoutsByMapContainerIndex =
                new Dictionary<int, NormalizedMapContainerLayout>();
            foreach (NormalizedMapContainerLayout layout in mapContainer.MapContainerLayouts ?? new List<NormalizedMapContainerLayout>())
            {
                if (layout == null)
                {
                    continue;
                }

                layoutsByMapContainerIndex[layout.MapContainerIndex] = layout;
            }

            return layoutsByMapContainerIndex;
        }

        private static NormalizedMapContainerLayout ResolveMapContainerLayout(
            IReadOnlyDictionary<int, NormalizedMapContainerLayout> layoutsByMapContainerIndex,
            int resolvedMapIndex)
        {
            if (layoutsByMapContainerIndex == null || resolvedMapIndex < 0)
            {
                return null;
            }

            return layoutsByMapContainerIndex.TryGetValue(resolvedMapIndex, out NormalizedMapContainerLayout layout)
                ? layout
                : null;
        }

        private static PermissionGridContract CreatePermissionGrid(
            string zoneToken,
            NormalizedMapContainerLayout mapContainerLayout)
        {
            if (mapContainerLayout == null ||
                mapContainerLayout.PermissionGridCandidates == null ||
                mapContainerLayout.PermissionGridCandidates.Count == 0)
            {
                return new PermissionGridContract
                {
                    GridId = $"{zoneToken}:permission-grid:unresolved",
                    Width = 0,
                    Height = 0,
                    CellTokens = Array.Empty<string>(),
                };
            }

            NormalizedPermissionGridCandidate candidate = mapContainerLayout.PermissionGridCandidates[0];
            if (candidate == null || candidate.Width <= 0 || candidate.Height <= 0)
            {
                return new PermissionGridContract
                {
                    GridId = $"{zoneToken}:permission-grid:unresolved",
                    Width = 0,
                    Height = 0,
                    CellTokens = Array.Empty<string>(),
                };
            }

            return new PermissionGridContract
            {
                GridId =
                    $"{zoneToken}:permission-grid:{mapContainerLayout.ContainerTag.ToLowerInvariant()}:s{candidate.SectionIndex}:p{candidate.PlaneCount}:t{candidate.TrailingRecordCount}",
                Width = candidate.Width,
                Height = candidate.Height,
                CellTokens = BuildPermissionGridCellTokens(candidate),
            };
        }

        private static string[] BuildPermissionGridCellTokens(NormalizedPermissionGridCandidate candidate)
        {
            if (candidate == null ||
                candidate.RecordTokens == null ||
                candidate.RecordTokens.Count == 0 ||
                candidate.PrimaryCellCount <= 0)
            {
                return Array.Empty<string>();
            }

            int planeCount = Math.Max(1, candidate.PlaneCount);
            int cellCount = candidate.PrimaryCellCount;
            List<string> cellTokens = new List<string>(cellCount);
            for (int cellIndex = 0; cellIndex < cellCount; cellIndex++)
            {
                if (planeCount == 1)
                {
                    cellTokens.Add(candidate.RecordTokens[cellIndex]);
                    continue;
                }

                List<string> planeTokens = new List<string>(planeCount);
                for (int planeIndex = 0; planeIndex < planeCount; planeIndex++)
                {
                    int recordIndex = cellIndex + (planeIndex * cellCount);
                    if (recordIndex >= candidate.RecordTokens.Count)
                    {
                        break;
                    }

                    planeTokens.Add(candidate.RecordTokens[recordIndex]);
                }

                cellTokens.Add(string.Join("|", planeTokens));
            }

            return cellTokens.ToArray();
        }

        private static WorldMapReferenceContract CreateMapReference(NormalizedMapLookupEntry lookupEntry)
        {
            return new WorldMapReferenceContract
            {
                LogicalMapIndex = lookupEntry.LogicalMapIndex,
                ResolvedMapIndex = lookupEntry.ResolvedMapIndex,
                IsIdentityMapping = lookupEntry.IsIdentityMapping,
            };
        }

        private static WorldMapSideLookupContract[] BuildMapSideLookups(NormalizedMapContainer mapSideLookupContainer)
        {
            if (mapSideLookupContainer.MapSideLookupEntries == null)
            {
                throw new InvalidDataException("Map side lookup container requires a mapSideLookupEntries list.");
            }

            List<WorldMapSideLookupContract> sideLookups = new List<WorldMapSideLookupContract>(mapSideLookupContainer.SideLookupEntryCount);
            foreach (NormalizedMapSideLookupEntry entry in mapSideLookupContainer.MapSideLookupEntries)
            {
                if (entry == null)
                {
                    throw new InvalidDataException("Map side lookup entries are required.");
                }

                sideLookups.Add(CreateMapSideLookup(entry));
            }

            if (mapSideLookupContainer.SideLookupEntryCount != sideLookups.Count)
            {
                throw new InvalidDataException(
                    $"Map side lookup container declares '{mapSideLookupContainer.SideLookupEntryCount}' entries, but decoded entry count is '{sideLookups.Count}'.");
            }

            return sideLookups.ToArray();
        }

        private static WorldMapSideLookupContract CreateMapSideLookup(NormalizedMapSideLookupEntry entry)
        {
            return new WorldMapSideLookupContract
            {
                EntryIndex = entry.EntryIndex,
                RawWord0 = entry.Word0,
                RawWord1 = entry.Word1,
            };
        }

        private static WorldMapSideLookupContract CloneMapSideLookup(WorldMapSideLookupContract source)
        {
            if (source == null)
            {
                return new WorldMapSideLookupContract();
            }

            return new WorldMapSideLookupContract
            {
                EntryIndex = source.EntryIndex,
                RawWord0 = source.RawWord0,
                RawWord1 = source.RawWord1,
            };
        }

        private static string[] CollectCandidateSceneIds(IReadOnlyList<WorldSceneContract> scenes)
        {
            List<string> sceneIds = new List<string>(scenes == null ? 0 : scenes.Count);
            foreach (WorldSceneContract scene in scenes ?? Array.Empty<WorldSceneContract>())
            {
                if (scene == null || string.IsNullOrWhiteSpace(scene.SceneId))
                {
                    continue;
                }

                sceneIds.Add(scene.SceneId);
            }

            return sceneIds.ToArray();
        }

        private static int[] CollectCandidateZoneIndices(IReadOnlyList<WorldSceneContract> scenes)
        {
            List<int> zoneIndices = new List<int>(scenes == null ? 0 : scenes.Count);
            foreach (WorldSceneContract scene in scenes ?? Array.Empty<WorldSceneContract>())
            {
                if (scene == null)
                {
                    continue;
                }

                zoneIndices.Add(scene.ZoneIndex);
            }

            return zoneIndices.ToArray();
        }

        private static Dictionary<int, NormalizedMapMetadataCandidate> BuildMetadataCandidatesByLogicalMapIndex(
            NormalizedMapContainer mapMetadataCandidateContainer)
        {
            Dictionary<int, NormalizedMapMetadataCandidate> candidatesByLogicalMapIndex =
                new Dictionary<int, NormalizedMapMetadataCandidate>();
            foreach (NormalizedMapMetadataCandidate candidate in mapMetadataCandidateContainer.MapMetadataCandidates ??
                new List<NormalizedMapMetadataCandidate>())
            {
                if (candidate == null)
                {
                    continue;
                }

                candidatesByLogicalMapIndex[candidate.LogicalMapIndex] = candidate;
            }

            return candidatesByLogicalMapIndex;
        }

        private static SeasonProfileContract[] CreateSeasonalVariants(NormalizedMapMetadataCandidate candidate)
        {
            if (candidate == null || candidate.SeasonProfiles == null || candidate.SeasonProfiles.Count == 0)
            {
                return Array.Empty<SeasonProfileContract>();
            }

            List<SeasonProfileContract> variants = new List<SeasonProfileContract>(candidate.SeasonProfiles.Count);
            foreach (NormalizedSeasonSlotProfile seasonProfile in candidate.SeasonProfiles)
            {
                if (seasonProfile == null)
                {
                    continue;
                }

                variants.Add(new SeasonProfileContract
                {
                    SeasonId = $"slot-{seasonProfile.SeasonSlotIndex}",
                    VariantTokens = CollectSeasonVariantTokens(seasonProfile),
                });
            }

            return variants.ToArray();
        }

        private static string[] CollectSeasonVariantTokens(NormalizedSeasonSlotProfile seasonProfile)
        {
            List<string> tokens = new List<string>();
            if (seasonProfile.NonZeroWords != null && seasonProfile.NonZeroWords.Count > 0)
            {
                foreach (NormalizedSeasonSlotWordValue wordValue in seasonProfile.NonZeroWords)
                {
                    if (wordValue == null)
                    {
                        continue;
                    }

                    tokens.Add($"w{wordValue.WordIndex:D2}:0x{wordValue.Value:X4}");
                }

                return tokens.ToArray();
            }

            if (seasonProfile.WordValues == null)
            {
                return Array.Empty<string>();
            }

            for (int index = 0; index < seasonProfile.WordValues.Count; index++)
            {
                int value = seasonProfile.WordValues[index];
                if (value == 0)
                {
                    continue;
                }

                tokens.Add($"w{index:D2}:0x{value:X4}");
            }

            return tokens.ToArray();
        }

        private static int CountScriptBindings(IReadOnlyList<WorldSceneContract> scenes)
        {
            int count = 0;
            foreach (WorldSceneContract scene in scenes)
            {
                if (scene == null)
                {
                    continue;
                }

                if (scene.PrimaryScriptMemberIndex >= 0)
                {
                    count += 1;
                }

                if (scene.SecondaryScriptMemberIndex >= 0)
                {
                    count += 1;
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
    }

    [Serializable]
    public sealed class Gen5WorldImportArtifactSet
    {
        public string RootPath { get; set; } = string.Empty;
        public string GeneratedAssetsRoot { get; set; } = string.Empty;
        public string ProfileAssetPath { get; set; } = string.Empty;
        public string ContentManifestAssetPath { get; set; } = string.Empty;
        public string ContentVersion { get; set; } = string.Empty;
        public string WorldDatabaseAssetPath { get; set; } = string.Empty;
        public int SceneCount { get; set; }
        public int MapReferenceCount { get; set; }
        public int MapRouteCount { get; set; }
        public int MapSideLookupCount { get; set; }
        public int ScriptBindingCount { get; set; }

        public string FormatSummary()
        {
            return
                $"Imported {SceneCount} world scenes, {MapSideLookupCount} map side lookups, and {ScriptBindingCount} script bindings from '{RootPath}' into '{WorldDatabaseAssetPath}', '{ProfileAssetPath}', and '{ContentManifestAssetPath}' (contentVersion={ContentVersion}).";
        }
    }

    internal sealed class WorldImportContractSet
    {
        public WorldSceneContract[] Scenes { get; set; } = Array.Empty<WorldSceneContract>();
        public WorldMapReferenceContract[] MapReferences { get; set; } = Array.Empty<WorldMapReferenceContract>();
        public WorldMapRouteContract[] MapRoutes { get; set; } = Array.Empty<WorldMapRouteContract>();
        public WorldMapSideLookupContract[] MapSideLookups { get; set; } = Array.Empty<WorldMapSideLookupContract>();
    }
}
