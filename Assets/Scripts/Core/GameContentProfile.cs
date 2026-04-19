using System;
using PokeBlack.Content.Runtime;
using PokeBlack2.Foundation.Runtime.Gen5.Contracts;
using UnityEngine;

namespace PokeBlack2.Foundation.Runtime.Core
{
    [CreateAssetMenu(menuName = "PokeBlack2/Game Content Profile", fileName = "GameContentProfile")]
    public sealed class GameContentProfile : ScriptableObject
    {
        public const string DefaultProfileId = "foundation-default";
        public const string DefaultContractFamily = "gen5-normalized";

        [SerializeField] private string profileId = DefaultProfileId;
        [SerializeField] private string contractFamily = DefaultContractFamily;
        [SerializeField] private GameVersion gameVersion = GameVersion.PokemonBlackUsaEurope;
        [SerializeField] private bool strictOfflineBoundaries = true;
        [SerializeField] private ContentManifest contentManifest;
        [SerializeField] private Gen5TextDatabaseAsset importedTextDatabase;
        [SerializeField] private Gen5ScriptDatabaseAsset importedScriptDatabase;
        [SerializeField] private Gen5WorldDatabaseAsset importedWorldDatabase;

        public string ProfileId => profileId;
        public string ContractFamily => contractFamily;
        public GameVersion GameVersion => gameVersion;
        public bool StrictOfflineBoundaries => strictOfflineBoundaries;
        public ContentManifest Manifest => contentManifest;
        public bool HasContentManifest => contentManifest != null;
        public Gen5TextDatabaseAsset TextDatabase => importedTextDatabase;
        public bool HasTextDatabase => importedTextDatabase != null;
        public Gen5ScriptDatabaseAsset ScriptDatabase => importedScriptDatabase;
        public bool HasScriptDatabase => importedScriptDatabase != null;
        public Gen5WorldDatabaseAsset WorldDatabase => importedWorldDatabase;
        public bool HasWorldDatabase => importedWorldDatabase != null;

        public static GameContentProfile CreateTransientDefault()
        {
            GameContentProfile instance = CreateInstance<GameContentProfile>();
            instance.name = "TransientGameContentProfile";
            return instance;
        }

        public void EnsureValid()
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                throw new InvalidOperationException("GameContentProfile requires a non-empty profile id.");
            }

            if (string.IsNullOrWhiteSpace(contractFamily))
            {
                throw new InvalidOperationException("GameContentProfile requires a non-empty contract family.");
            }

            if (!strictOfflineBoundaries)
            {
                throw new InvalidOperationException("Foundation profiles must keep strict offline boundaries enabled.");
            }

            if (contentManifest != null)
            {
                contentManifest.EnsureValid();
                if (!string.Equals(contentManifest.ContractFamily, contractFamily, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"GameContentProfile contract family '{contractFamily}' does not match ContentManifest contract family '{contentManifest.ContractFamily}'.");
                }

                if (!string.Equals(contentManifest.ProfileId, profileId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"GameContentProfile profile id '{profileId}' does not match ContentManifest profile id '{contentManifest.ProfileId}'.");
                }
            }
        }

        public ContentManifest LoadContentManifest()
        {
            return contentManifest;
        }

        public Gen5TextDatabaseAsset LoadTextDatabase()
        {
            return importedTextDatabase;
        }

        public Gen5ScriptDatabaseAsset LoadScriptDatabase()
        {
            return importedScriptDatabase;
        }

        public Gen5WorldDatabaseAsset LoadWorldDatabase()
        {
            return importedWorldDatabase;
        }

        public void ApplyImportedTextDatabase(Gen5TextDatabaseAsset textDatabase)
        {
            importedTextDatabase = textDatabase;
        }

        public void ApplyContentManifest(ContentManifest manifest)
        {
            contentManifest = manifest;
        }

        public void ApplyImportedScriptDatabase(Gen5ScriptDatabaseAsset scriptDatabase)
        {
            importedScriptDatabase = scriptDatabase;
        }

        public void ApplyImportedWorldDatabase(Gen5WorldDatabaseAsset worldDatabase)
        {
            importedWorldDatabase = worldDatabase;
        }
    }
}
