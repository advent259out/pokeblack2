using System;
using PokeBlack2.Foundation.Runtime.Gen5.Contracts;
using UnityEngine;

namespace PokeBlack2.Foundation.Runtime.Core
{
    [CreateAssetMenu(menuName = "PokeBlack2/Game Content Profile", fileName = "GameContentProfile")]
    public sealed class GameContentProfile : ScriptableObject
    {
        [SerializeField] private string profileId = "foundation-default";
        [SerializeField] private string contractFamily = "gen5-normalized";
        [SerializeField] private GameVersion gameVersion = GameVersion.PokemonBlackUsaEurope;
        [SerializeField] private bool strictOfflineBoundaries = true;
        [SerializeField] private Gen5TextDatabaseAsset importedTextDatabase;
        [SerializeField] private Gen5ScriptDatabaseAsset importedScriptDatabase;
        [SerializeField] private Gen5WorldDatabaseAsset importedWorldDatabase;

        public string ProfileId => profileId;
        public string ContractFamily => contractFamily;
        public GameVersion GameVersion => gameVersion;
        public bool StrictOfflineBoundaries => strictOfflineBoundaries;
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
