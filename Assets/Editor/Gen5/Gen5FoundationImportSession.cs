using System;
using System.Collections.Generic;
using System.IO;

namespace PokeBlack2.Foundation.Editor
{
    public sealed class Gen5FoundationImportSession
    {
        private readonly Dictionary<string, NormalizedGroupIndex> groupIndexCache =
            new Dictionary<string, NormalizedGroupIndex>(StringComparer.Ordinal);

        private readonly Dictionary<string, IReadOnlyList<NormalizedSourceCatalogEntry>> sourcesByGroup =
            new Dictionary<string, IReadOnlyList<NormalizedSourceCatalogEntry>>(StringComparer.Ordinal);

        private readonly IReadOnlyList<string> availableGroups;
        private readonly NormalizedContractRegistry registry;
        private NormalizedMapGroupIndex mapGroupIndexCache;
        private NormalizedScriptGroupIndex scriptGroupIndexCache;
        private NormalizedTextGroupIndex textGroupIndexCache;

        private Gen5FoundationImportSession(
            NormalizedContractRegistry registry,
            NormalizedRomInfo romInfo,
            NormalizedSourceCatalog sourceCatalog)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            RomInfo = romInfo ?? throw new ArgumentNullException(nameof(romInfo));
            SourceCatalog = sourceCatalog ?? throw new ArgumentNullException(nameof(sourceCatalog));

            ValidateRomConsistency();
            ValidateSourceCatalogConsistency();

            foreach (string groupName in Gen5ImportProfile.GetSupportedNormalizedGroups())
            {
                List<NormalizedSourceCatalogEntry> groupSources = new List<NormalizedSourceCatalogEntry>();
                foreach (NormalizedSourceCatalogEntry source in SourceCatalog.Sources)
                {
                    if (string.Equals(source.Group, groupName, StringComparison.Ordinal))
                    {
                        groupSources.Add(source);
                    }
                }

                sourcesByGroup[groupName] = groupSources.AsReadOnly();
            }

            List<string> discoveredGroups = new List<string>();
            foreach (string groupName in Gen5ImportProfile.GetSupportedNormalizedGroups())
            {
                if (registry.ContainsRootRelativeOutput(Gen5ImportProfile.GetGroupIndexRelativePath(groupName)))
                {
                    discoveredGroups.Add(groupName);
                }
            }

            availableGroups = discoveredGroups.AsReadOnly();
        }

        public ExtractionManifest Manifest => registry.Manifest;
        public string RootPath => registry.RootPath;
        public NormalizedRomInfo RomInfo { get; }
        public NormalizedSourceCatalog SourceCatalog { get; }
        public IReadOnlyList<string> AvailableGroups => availableGroups;

        public static Gen5FoundationImportSession LoadCanonical()
        {
            return LoadFromRoot(Gen5ImportProfile.CanonicalExportRoot);
        }

        public static Gen5FoundationImportSession LoadFromRoot(string rootPath)
        {
            NormalizedContractRegistry registry = NormalizedContractRegistry.LoadFromRoot(rootPath);
            return new Gen5FoundationImportSession(
                registry,
                registry.LoadRomInfo(),
                registry.LoadSourceCatalog());
        }

        public bool HasGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                return false;
            }

            foreach (string availableGroup in availableGroups)
            {
                if (string.Equals(availableGroup, groupName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<NormalizedSourceCatalogEntry> GetSourcesForGroup(string groupName)
        {
            if (!sourcesByGroup.TryGetValue(groupName, out IReadOnlyList<NormalizedSourceCatalogEntry> groupSources))
            {
                throw new ArgumentException($"Unsupported normalized group '{groupName}'.", nameof(groupName));
            }

            return groupSources;
        }

        public NormalizedGroupIndex LoadGroupIndex(string groupName)
        {
            if (!HasGroup(groupName))
            {
                throw new InvalidOperationException($"Normalized group '{groupName}' is not available in the current import session.");
            }

            if (groupIndexCache.TryGetValue(groupName, out NormalizedGroupIndex cachedIndex))
            {
                return cachedIndex;
            }

            NormalizedGroupIndex groupIndex = registry.LoadGroupIndex(groupName);
            ValidateGroupIndex(groupName, groupIndex);
            groupIndexCache[groupName] = groupIndex;
            return groupIndex;
        }

        public NormalizedTextGroupIndex LoadTextGroupIndex()
        {
            if (!HasGroup("text"))
            {
                throw new InvalidOperationException("Normalized group 'text' is not available in the current import session.");
            }

            if (textGroupIndexCache != null)
            {
                return textGroupIndexCache;
            }

            NormalizedTextGroupIndex textGroupIndex = registry.LoadTextGroupIndex();
            ValidateTextGroupIndex(textGroupIndex);
            textGroupIndexCache = textGroupIndex;
            return textGroupIndex;
        }

        public NormalizedMapGroupIndex LoadMapGroupIndex()
        {
            if (!HasGroup("maps"))
            {
                throw new InvalidOperationException("Normalized group 'maps' is not available in the current import session.");
            }

            if (mapGroupIndexCache != null)
            {
                return mapGroupIndexCache;
            }

            NormalizedMapGroupIndex mapGroupIndex = registry.LoadMapGroupIndex();
            ValidateMapGroupIndex(mapGroupIndex);
            mapGroupIndexCache = mapGroupIndex;
            return mapGroupIndex;
        }

        public NormalizedScriptGroupIndex LoadScriptGroupIndex()
        {
            if (!HasGroup("scripts"))
            {
                throw new InvalidOperationException("Normalized group 'scripts' is not available in the current import session.");
            }

            if (scriptGroupIndexCache != null)
            {
                return scriptGroupIndexCache;
            }

            NormalizedScriptGroupIndex scriptGroupIndex = registry.LoadScriptGroupIndex();
            ValidateScriptGroupIndex(scriptGroupIndex);
            scriptGroupIndexCache = scriptGroupIndex;
            return scriptGroupIndex;
        }

        private void ValidateRomConsistency()
        {
            if (!string.Equals(RomInfo.Filename, Manifest.Rom.Filename, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Normalized rom-info filename '{RomInfo.Filename}' does not match manifest rom filename '{Manifest.Rom.Filename}'.");
            }

            if (!string.Equals(RomInfo.Sha1, Manifest.Rom.Sha1, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Normalized rom-info sha1 '{RomInfo.Sha1}' does not match manifest rom sha1 '{Manifest.Rom.Sha1}'.");
            }

            if (RomInfo.Size != Manifest.Rom.Size)
            {
                throw new InvalidDataException(
                    $"Normalized rom-info size '{RomInfo.Size}' does not match manifest rom size '{Manifest.Rom.Size}'.");
            }
        }

        private void ValidateSourceCatalogConsistency()
        {
            if (SourceCatalog.Rom == null)
            {
                throw new InvalidDataException("Normalized source-catalog rom block is required.");
            }

            if (SourceCatalog.Sources == null)
            {
                throw new InvalidDataException("Normalized source-catalog sources list is required.");
            }

            if (!string.Equals(SourceCatalog.Rom.Filename, Manifest.Rom.Filename, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Normalized source-catalog filename '{SourceCatalog.Rom.Filename}' does not match manifest rom filename '{Manifest.Rom.Filename}'.");
            }

            if (!string.Equals(SourceCatalog.Rom.Sha1, Manifest.Rom.Sha1, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Normalized source-catalog sha1 '{SourceCatalog.Rom.Sha1}' does not match manifest rom sha1 '{Manifest.Rom.Sha1}'.");
            }

            if (SourceCatalog.Rom.Size != Manifest.Rom.Size)
            {
                throw new InvalidDataException(
                    $"Normalized source-catalog size '{SourceCatalog.Rom.Size}' does not match manifest rom size '{Manifest.Rom.Size}'.");
            }

            if (SourceCatalog.SourceCount != SourceCatalog.Sources.Count)
            {
                throw new InvalidDataException(
                    $"Normalized source-catalog count '{SourceCatalog.SourceCount}' does not match the source entry count '{SourceCatalog.Sources.Count}'.");
            }
        }

        private static void ValidateGroupIndex(string groupName, NormalizedGroupIndex groupIndex)
        {
            if (groupIndex.Containers == null)
            {
                throw new InvalidDataException($"Normalized group '{groupName}' containers list is required.");
            }

            if (!string.Equals(groupIndex.Group, groupName, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Normalized group index '{groupIndex.Group}' does not match requested group '{groupName}'.");
            }

            if (groupIndex.ContainerCount != groupIndex.Containers.Count)
            {
                throw new InvalidDataException(
                    $"Normalized group '{groupName}' container count '{groupIndex.ContainerCount}' does not match the container entry count '{groupIndex.Containers.Count}'.");
            }
        }

        private static void ValidateMapGroupIndex(NormalizedMapGroupIndex groupIndex)
        {
            if (groupIndex == null)
            {
                throw new InvalidDataException("Normalized map group index is required.");
            }

            if (!string.Equals(groupIndex.Group, "maps", StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Normalized map group index '{groupIndex.Group}' does not match expected group 'maps'.");
            }

            if (groupIndex.Containers == null)
            {
                throw new InvalidDataException("Normalized map group containers list is required.");
            }

            if (groupIndex.ContainerCount != groupIndex.Containers.Count)
            {
                throw new InvalidDataException(
                    $"Normalized map group container count '{groupIndex.ContainerCount}' does not match the container entry count '{groupIndex.Containers.Count}'.");
            }

            int totalScriptTextBindings = 0;
            foreach (NormalizedMapContainer container in groupIndex.Containers)
            {
                if (container == null)
                {
                    throw new InvalidDataException("Normalized map container entries are required.");
                }

                if (container.Members == null)
                {
                    throw new InvalidDataException($"Normalized map container '{container.Id}' requires a members list.");
                }

                if (container.MemberCount != container.Members.Count)
                {
                    throw new InvalidDataException(
                        $"Normalized map container '{container.Id}' member count '{container.MemberCount}' does not match the member entry count '{container.Members.Count}'.");
                }

                int containerScriptTextBindingCount = container.ScriptTextBindings == null ? 0 : container.ScriptTextBindings.Count;
                if (container.ScriptTextBindingCount != containerScriptTextBindingCount)
                {
                    throw new InvalidDataException(
                        $"Normalized map container '{container.Id}' script text binding count '{container.ScriptTextBindingCount}' does not match the binding entry count '{containerScriptTextBindingCount}'.");
                }

                int containerZoneCount = container.Zones == null ? 0 : container.Zones.Count;
                if (container.ZoneCount != containerZoneCount)
                {
                    throw new InvalidDataException(
                        $"Normalized map container '{container.Id}' zone count '{container.ZoneCount}' does not match the zone entry count '{containerZoneCount}'.");
                }

                if (string.Equals(container.Id, "map-lookup", StringComparison.Ordinal))
                {
                    int mapLookupEntryCount = container.MapLookupEntries == null ? 0 : container.MapLookupEntries.Count;
                    if (container.LookupCount != mapLookupEntryCount)
                    {
                        throw new InvalidDataException(
                            $"Normalized map container '{container.Id}' lookup count '{container.LookupCount}' does not match the map lookup entry count '{mapLookupEntryCount}'.");
                    }

                    int identityMappingCount = 0;
                    int aliasMappingCount = 0;
                    HashSet<int> canonicalMapIndices = new HashSet<int>();
                    int expectedLogicalMapIndex = 0;
                    foreach (NormalizedMapLookupEntry lookupEntry in container.MapLookupEntries ?? new List<NormalizedMapLookupEntry>())
                    {
                        if (lookupEntry == null)
                        {
                            throw new InvalidDataException($"Normalized map lookup entry in container '{container.Id}' is required.");
                        }

                        if (lookupEntry.LogicalMapIndex != expectedLogicalMapIndex)
                        {
                            throw new InvalidDataException(
                                $"Normalized map lookup entry ordering in container '{container.Id}' expected logical map index '{expectedLogicalMapIndex}', but decoded '{lookupEntry.LogicalMapIndex}'.");
                        }

                        if (lookupEntry.ResolvedMapIndex < 0)
                        {
                            throw new InvalidDataException(
                                $"Normalized map lookup entry '{lookupEntry.LogicalMapIndex}' in container '{container.Id}' requires a non-negative resolved map index, but decoded '{lookupEntry.ResolvedMapIndex}'.");
                        }

                        bool isIdentityMapping = lookupEntry.LogicalMapIndex == lookupEntry.ResolvedMapIndex;
                        if (lookupEntry.IsIdentityMapping != isIdentityMapping)
                        {
                            throw new InvalidDataException(
                                $"Normalized map lookup entry '{lookupEntry.LogicalMapIndex}' in container '{container.Id}' declares identity mapping '{lookupEntry.IsIdentityMapping}', but the resolved map index '{lookupEntry.ResolvedMapIndex}' implies '{isIdentityMapping}'.");
                        }

                        if (lookupEntry.IsIdentityMapping)
                        {
                            identityMappingCount += 1;
                        }
                        else
                        {
                            aliasMappingCount += 1;
                        }

                        canonicalMapIndices.Add(lookupEntry.ResolvedMapIndex);
                        expectedLogicalMapIndex += 1;
                    }

                    if (container.IdentityMappingCount != identityMappingCount)
                    {
                        throw new InvalidDataException(
                            $"Normalized map container '{container.Id}' identity mapping count '{container.IdentityMappingCount}' does not match the decoded identity entry count '{identityMappingCount}'.");
                    }

                    if (container.AliasMappingCount != aliasMappingCount)
                    {
                        throw new InvalidDataException(
                            $"Normalized map container '{container.Id}' alias mapping count '{container.AliasMappingCount}' does not match the decoded alias entry count '{aliasMappingCount}'.");
                    }

                    if (container.CanonicalMapCount != canonicalMapIndices.Count)
                    {
                        throw new InvalidDataException(
                            $"Normalized map container '{container.Id}' canonical map count '{container.CanonicalMapCount}' does not match the unique resolved map count '{canonicalMapIndices.Count}'.");
                    }

                    int maxResolvedMapIndex = -1;
                    foreach (int resolvedMapIndex in canonicalMapIndices)
                    {
                        maxResolvedMapIndex = Math.Max(maxResolvedMapIndex, resolvedMapIndex);
                    }

                    if (container.MaxResolvedMapIndex != maxResolvedMapIndex)
                    {
                        throw new InvalidDataException(
                            $"Normalized map container '{container.Id}' max resolved map index '{container.MaxResolvedMapIndex}' does not match the decoded max '{maxResolvedMapIndex}'.");
                    }
                }

                if (string.Equals(container.Id, "map-containers", StringComparison.Ordinal))
                {
                    int mapContainerLayoutCount = container.MapContainerLayouts == null ? 0 : container.MapContainerLayouts.Count;
                    if (container.MapContainerLayoutCount != mapContainerLayoutCount)
                    {
                        throw new InvalidDataException(
                            $"Normalized map container '{container.Id}' layout count '{container.MapContainerLayoutCount}' does not match the layout entry count '{mapContainerLayoutCount}'.");
                    }

                    if (container.MemberCount != mapContainerLayoutCount)
                    {
                        throw new InvalidDataException(
                            $"Normalized map container '{container.Id}' member count '{container.MemberCount}' does not match the map container layout entry count '{mapContainerLayoutCount}'.");
                    }

                    int permissionGridCandidateMapCount = 0;
                    int permissionGridCandidateCount = 0;
                    int expectedMapContainerIndex = 0;
                    foreach (NormalizedMapContainerLayout layout in container.MapContainerLayouts ?? new List<NormalizedMapContainerLayout>())
                    {
                        if (layout == null)
                        {
                            throw new InvalidDataException($"Normalized map container layout in container '{container.Id}' is required.");
                        }

                        if (layout.MapContainerIndex != expectedMapContainerIndex)
                        {
                            throw new InvalidDataException(
                                $"Normalized map container layout ordering in container '{container.Id}' expected map container index '{expectedMapContainerIndex}', but decoded '{layout.MapContainerIndex}'.");
                        }

                        int sectionCount = layout.Sections == null ? 0 : layout.Sections.Count;
                        if (layout.SectionCount != sectionCount)
                        {
                            throw new InvalidDataException(
                                $"Normalized map container layout '{layout.MapContainerIndex}' in container '{container.Id}' section count '{layout.SectionCount}' does not match the section entry count '{sectionCount}'.");
                        }

                        int modelSectionCount = 0;
                        foreach (NormalizedMapContainerSection section in layout.Sections ?? new List<NormalizedMapContainerSection>())
                        {
                            if (section == null)
                            {
                                throw new InvalidDataException(
                                    $"Normalized map container section in layout '{layout.MapContainerIndex}' is required.");
                            }

                            if (section.SectionIndex < 0 || section.SectionIndex >= layout.SectionCount)
                            {
                                throw new InvalidDataException(
                                    $"Normalized map container section '{section.SectionIndex}' in layout '{layout.MapContainerIndex}' is outside the declared section count '{layout.SectionCount}'.");
                            }

                            if (section.Size < 0 || section.Offset < 0)
                            {
                                throw new InvalidDataException(
                                    $"Normalized map container section '{section.SectionIndex}' in layout '{layout.MapContainerIndex}' requires non-negative offset and size.");
                            }

                            if (section.StartsWithModelMagic)
                            {
                                modelSectionCount += 1;
                            }
                        }

                        if (layout.ModelSectionCount != modelSectionCount)
                        {
                            throw new InvalidDataException(
                                $"Normalized map container layout '{layout.MapContainerIndex}' model section count '{layout.ModelSectionCount}' does not match the decoded model section count '{modelSectionCount}'.");
                        }

                        int layoutPermissionGridCandidateCount = layout.PermissionGridCandidates == null ? 0 : layout.PermissionGridCandidates.Count;
                        if (layout.PermissionGridCandidateCount != layoutPermissionGridCandidateCount)
                        {
                            throw new InvalidDataException(
                                $"Normalized map container layout '{layout.MapContainerIndex}' permission grid candidate count '{layout.PermissionGridCandidateCount}' does not match the candidate entry count '{layoutPermissionGridCandidateCount}'.");
                        }

                        foreach (NormalizedPermissionGridCandidate candidate in layout.PermissionGridCandidates ?? new List<NormalizedPermissionGridCandidate>())
                        {
                            if (candidate == null)
                            {
                                throw new InvalidDataException(
                                    $"Normalized permission grid candidate in layout '{layout.MapContainerIndex}' is required.");
                            }

                            if (candidate.Width <= 0 || candidate.Height <= 0)
                            {
                                throw new InvalidDataException(
                                    $"Normalized permission grid candidate in layout '{layout.MapContainerIndex}' requires positive width and height, but decoded '{candidate.Width}x{candidate.Height}'.");
                            }

                            if (candidate.PrimaryCellCount != candidate.Width * candidate.Height)
                            {
                                throw new InvalidDataException(
                                    $"Normalized permission grid candidate in layout '{layout.MapContainerIndex}' primary cell count '{candidate.PrimaryCellCount}' does not match width*height '{candidate.Width * candidate.Height}'.");
                            }

                            if (candidate.RecordStrideBytes != 8)
                            {
                                throw new InvalidDataException(
                                    $"Normalized permission grid candidate in layout '{layout.MapContainerIndex}' record stride '{candidate.RecordStrideBytes}' must remain 8 bytes in this foundation phase.");
                            }

                            int recordTokenCount = candidate.RecordTokens == null ? 0 : candidate.RecordTokens.Count;
                            if (candidate.RecordTokenCount != recordTokenCount)
                            {
                                throw new InvalidDataException(
                                    $"Normalized permission grid candidate in layout '{layout.MapContainerIndex}' record token count '{candidate.RecordTokenCount}' does not match the token entry count '{recordTokenCount}'.");
                            }

                            if (candidate.RecordCount != recordTokenCount)
                            {
                                throw new InvalidDataException(
                                    $"Normalized permission grid candidate in layout '{layout.MapContainerIndex}' record count '{candidate.RecordCount}' does not match the token entry count '{recordTokenCount}'.");
                            }

                            if (candidate.PlaneCount <= 0)
                            {
                                throw new InvalidDataException(
                                    $"Normalized permission grid candidate in layout '{layout.MapContainerIndex}' requires at least one plane, but decoded '{candidate.PlaneCount}'.");
                            }

                            if ((candidate.PlaneCount * candidate.PrimaryCellCount) + candidate.TrailingRecordCount != candidate.RecordCount)
                            {
                                throw new InvalidDataException(
                                    $"Normalized permission grid candidate in layout '{layout.MapContainerIndex}' record decomposition is inconsistent with plane count '{candidate.PlaneCount}', primary cell count '{candidate.PrimaryCellCount}', trailing count '{candidate.TrailingRecordCount}', and record count '{candidate.RecordCount}'.");
                            }
                        }

                        if (layout.PermissionGridCandidateCount > 0)
                        {
                            permissionGridCandidateMapCount += 1;
                        }

                        permissionGridCandidateCount += layout.PermissionGridCandidateCount;
                        expectedMapContainerIndex += 1;
                    }

                    if (container.PermissionGridCandidateMapCount != permissionGridCandidateMapCount)
                    {
                        throw new InvalidDataException(
                            $"Normalized map container '{container.Id}' permission grid candidate map count '{container.PermissionGridCandidateMapCount}' does not match the decoded map count '{permissionGridCandidateMapCount}'.");
                    }

                    if (container.PermissionGridCandidateCount != permissionGridCandidateCount)
                    {
                        throw new InvalidDataException(
                            $"Normalized map container '{container.Id}' permission grid candidate count '{container.PermissionGridCandidateCount}' does not match the decoded candidate count '{permissionGridCandidateCount}'.");
                    }
                }

                if (string.Equals(container.Id, "map-side-lookup-candidate", StringComparison.Ordinal))
                {
                    int mapSideLookupEntryCount = container.MapSideLookupEntries == null ? 0 : container.MapSideLookupEntries.Count;
                    if (container.SideLookupEntryCount != mapSideLookupEntryCount)
                    {
                        throw new InvalidDataException(
                            $"Normalized map container '{container.Id}' side lookup count '{container.SideLookupEntryCount}' does not match the side lookup entry count '{mapSideLookupEntryCount}'.");
                    }

                    if (container.MemberCount != mapSideLookupEntryCount)
                    {
                        throw new InvalidDataException(
                            $"Normalized map container '{container.Id}' member count '{container.MemberCount}' does not match the side lookup entry count '{mapSideLookupEntryCount}'.");
                    }

                    HashSet<string> distinctPairs = new HashSet<string>(StringComparer.Ordinal);
                    int expectedEntryIndex = 0;
                    foreach (NormalizedMapSideLookupEntry sideLookupEntry in container.MapSideLookupEntries ?? new List<NormalizedMapSideLookupEntry>())
                    {
                        if (sideLookupEntry == null)
                        {
                            throw new InvalidDataException($"Normalized map side lookup entry in container '{container.Id}' is required.");
                        }

                        if (sideLookupEntry.EntryIndex != expectedEntryIndex)
                        {
                            throw new InvalidDataException(
                                $"Normalized map side lookup ordering in container '{container.Id}' expected entry index '{expectedEntryIndex}', but decoded '{sideLookupEntry.EntryIndex}'.");
                        }

                        if (sideLookupEntry.Word0 < 0 || sideLookupEntry.Word1 < 0)
                        {
                            throw new InvalidDataException(
                                $"Normalized map side lookup entry '{sideLookupEntry.EntryIndex}' in container '{container.Id}' requires non-negative words, but decoded '{sideLookupEntry.Word0}' and '{sideLookupEntry.Word1}'.");
                        }

                        distinctPairs.Add($"{sideLookupEntry.Word0}:{sideLookupEntry.Word1}");
                        expectedEntryIndex += 1;
                    }

                    if (container.DistinctSideLookupPairCount != distinctPairs.Count)
                    {
                        throw new InvalidDataException(
                            $"Normalized map container '{container.Id}' distinct side lookup pair count '{container.DistinctSideLookupPairCount}' does not match the decoded distinct pair count '{distinctPairs.Count}'.");
                    }
                }

                foreach (NormalizedZoneHeader zone in container.Zones ?? new List<NormalizedZoneHeader>())
                {
                    if (zone == null)
                    {
                        throw new InvalidDataException($"Normalized zone header entry in container '{container.Id}' is required.");
                    }

                    int zoneBindingCount = zone.ScriptTextBindings == null ? 0 : zone.ScriptTextBindings.Count;
                    if (zone.ScriptTextBindingCount != zoneBindingCount)
                    {
                        throw new InvalidDataException(
                            $"Normalized zone '{zone.ZoneIndex}' in container '{container.Id}' script text binding count '{zone.ScriptTextBindingCount}' does not match the binding entry count '{zoneBindingCount}'.");
                    }
                }

                totalScriptTextBindings += containerScriptTextBindingCount;
            }

            if (groupIndex.TotalScriptTextBindings != totalScriptTextBindings)
            {
                throw new InvalidDataException(
                    $"Normalized map group total script text bindings '{groupIndex.TotalScriptTextBindings}' does not match the binding entry count '{totalScriptTextBindings}'.");
            }
        }

        private static void ValidateTextGroupIndex(NormalizedTextGroupIndex groupIndex)
        {
            if (groupIndex == null)
            {
                throw new InvalidDataException("Normalized text group index is required.");
            }

            if (!string.Equals(groupIndex.Group, "text", StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Normalized text group index '{groupIndex.Group}' does not match expected group 'text'.");
            }

            if (groupIndex.Containers == null)
            {
                throw new InvalidDataException("Normalized text group containers list is required.");
            }

            if (groupIndex.ContainerCount != groupIndex.Containers.Count)
            {
                throw new InvalidDataException(
                    $"Normalized text group container count '{groupIndex.ContainerCount}' does not match the container entry count '{groupIndex.Containers.Count}'.");
            }
        }

        private static void ValidateScriptGroupIndex(NormalizedScriptGroupIndex groupIndex)
        {
            if (groupIndex == null)
            {
                throw new InvalidDataException("Normalized script group index is required.");
            }

            if (!string.Equals(groupIndex.Group, "scripts", StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Normalized script group index '{groupIndex.Group}' does not match expected group 'scripts'.");
            }

            if (groupIndex.Containers == null)
            {
                throw new InvalidDataException("Normalized script group containers list is required.");
            }

            if (groupIndex.ContainerCount != groupIndex.Containers.Count)
            {
                throw new InvalidDataException(
                    $"Normalized script group container count '{groupIndex.ContainerCount}' does not match the container entry count '{groupIndex.Containers.Count}'.");
            }
        }
    }
}
