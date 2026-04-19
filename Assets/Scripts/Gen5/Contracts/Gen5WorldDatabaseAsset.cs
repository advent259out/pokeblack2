using System;
using System.Collections.Generic;
using PokeBlack2.Foundation.Runtime.Core;
using UnityEngine;

namespace PokeBlack2.Foundation.Runtime.Gen5.Contracts
{
    [CreateAssetMenu(menuName = "PokeBlack2/Gen5 World Database", fileName = "Gen5WorldDatabase")]
    public sealed class Gen5WorldDatabaseAsset : ScriptableObject
    {
        [SerializeField] private string exportRoot = string.Empty;
        [SerializeField] private GameVersion gameVersion = GameVersion.PokemonBlackUsaEurope;
        [SerializeField] private string romFilename = string.Empty;
        [SerializeField] private string romSha1 = string.Empty;
        [SerializeField] private WorldSceneContract[] scenes = Array.Empty<WorldSceneContract>();
        [SerializeField] private WorldMapReferenceContract[] mapReferences = Array.Empty<WorldMapReferenceContract>();
        [SerializeField] private WorldMapRouteContract[] mapRoutes = Array.Empty<WorldMapRouteContract>();
        [SerializeField] private WorldMapSideLookupContract[] mapSideLookups = Array.Empty<WorldMapSideLookupContract>();

        public string ExportRoot => exportRoot;
        public GameVersion GameVersion => gameVersion;
        public string RomFilename => romFilename;
        public string RomSha1 => romSha1;
        public IReadOnlyList<WorldSceneContract> Scenes => scenes;
        public IReadOnlyList<WorldMapReferenceContract> MapReferences => mapReferences;
        public IReadOnlyList<WorldMapRouteContract> MapRoutes => mapRoutes;
        public IReadOnlyList<WorldMapSideLookupContract> MapSideLookups => mapSideLookups;

        public int SceneCount => scenes == null ? 0 : scenes.Length;
        public int MapReferenceCount => mapReferences == null ? 0 : mapReferences.Length;
        public int MapRouteCount => mapRoutes == null ? 0 : mapRoutes.Length;
        public int MapSideLookupCount => mapSideLookups == null ? 0 : mapSideLookups.Length;

        public int ScriptBindingCount
        {
            get
            {
                if (scenes == null)
                {
                    return 0;
                }

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
        }

        public void Configure(
            string exportRoot,
            GameVersion gameVersion,
            string romFilename,
            string romSha1,
            WorldSceneContract[] scenes,
            WorldMapReferenceContract[] mapReferences,
            WorldMapRouteContract[] mapRoutes,
            WorldMapSideLookupContract[] mapSideLookups)
        {
            this.exportRoot = exportRoot ?? string.Empty;
            this.gameVersion = gameVersion;
            this.romFilename = romFilename ?? string.Empty;
            this.romSha1 = romSha1 ?? string.Empty;
            this.scenes = scenes ?? Array.Empty<WorldSceneContract>();
            this.mapReferences = mapReferences ?? Array.Empty<WorldMapReferenceContract>();
            this.mapRoutes = mapRoutes ?? Array.Empty<WorldMapRouteContract>();
            this.mapSideLookups = mapSideLookups ?? Array.Empty<WorldMapSideLookupContract>();
        }

        public bool TryGetScene(string sceneId, out WorldSceneContract scene)
        {
            if (scenes != null)
            {
                foreach (WorldSceneContract candidate in scenes)
                {
                    if (candidate != null &&
                        string.Equals(candidate.SceneId, sceneId, StringComparison.Ordinal))
                    {
                        scene = candidate;
                        return true;
                    }
                }
            }

            scene = null;
            return false;
        }

        public bool TryGetSceneByZoneIndex(int zoneIndex, out WorldSceneContract scene)
        {
            if (scenes != null)
            {
                foreach (WorldSceneContract candidate in scenes)
                {
                    if (candidate != null && candidate.ZoneIndex == zoneIndex)
                    {
                        scene = candidate;
                        return true;
                    }
                }
            }

            scene = null;
            return false;
        }

        public bool TryGetMapReference(int logicalMapIndex, out WorldMapReferenceContract mapReference)
        {
            if (mapReferences != null)
            {
                foreach (WorldMapReferenceContract candidate in mapReferences)
                {
                    if (candidate != null && candidate.LogicalMapIndex == logicalMapIndex)
                    {
                        mapReference = candidate;
                        return true;
                    }
                }
            }

            mapReference = null;
            return false;
        }

        public bool TryGetSceneForLogicalMapIndex(int logicalMapIndex, out WorldSceneContract scene)
        {
            if (TryGetDirectSceneForLogicalMapIndex(logicalMapIndex, out scene))
            {
                return true;
            }

            if (TryGetMapRoute(logicalMapIndex, out WorldMapRouteContract route) &&
                route.CandidateSceneIds != null &&
                route.CandidateSceneIds.Length == 1 &&
                TryGetScene(route.CandidateSceneIds[0], out scene))
            {
                return true;
            }

            scene = null;
            return false;
        }

        public bool TryGetMapRoute(int logicalMapIndex, out WorldMapRouteContract mapRoute)
        {
            if (mapRoutes != null)
            {
                foreach (WorldMapRouteContract candidate in mapRoutes)
                {
                    if (candidate != null && candidate.LogicalMapIndex == logicalMapIndex)
                    {
                        mapRoute = candidate;
                        return true;
                    }
                }
            }

            mapRoute = null;
            return false;
        }

        public bool TryGetMapSideLookup(int entryIndex, out WorldMapSideLookupContract mapSideLookup)
        {
            if (mapSideLookups != null)
            {
                foreach (WorldMapSideLookupContract candidate in mapSideLookups)
                {
                    if (candidate != null && candidate.EntryIndex == entryIndex)
                    {
                        mapSideLookup = candidate;
                        return true;
                    }
                }
            }

            mapSideLookup = null;
            return false;
        }

        public bool TryGetCandidateScenesForLogicalMapIndex(int logicalMapIndex, out WorldSceneContract[] candidateScenes)
        {
            if (!TryGetMapRoute(logicalMapIndex, out WorldMapRouteContract route) ||
                route.CandidateSceneIds == null ||
                route.CandidateSceneIds.Length == 0)
            {
                candidateScenes = Array.Empty<WorldSceneContract>();
                return false;
            }

            WorldSceneContract[] resolvedScenes = new WorldSceneContract[route.CandidateSceneIds.Length];
            for (int index = 0; index < route.CandidateSceneIds.Length; index++)
            {
                if (!TryGetScene(route.CandidateSceneIds[index], out WorldSceneContract candidateScene))
                {
                    candidateScenes = Array.Empty<WorldSceneContract>();
                    return false;
                }

                resolvedScenes[index] = candidateScene;
            }

            candidateScenes = resolvedScenes;
            return true;
        }

        private bool TryGetDirectSceneForLogicalMapIndex(int logicalMapIndex, out WorldSceneContract scene)
        {
            if (scenes != null)
            {
                foreach (WorldSceneContract candidate in scenes)
                {
                    if (candidate != null &&
                        candidate.MapReference != null &&
                        candidate.MapReference.LogicalMapIndex == logicalMapIndex)
                    {
                        scene = candidate;
                        return true;
                    }
                }
            }

            scene = null;
            return false;
        }
    }
}
