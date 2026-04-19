using PokeBlack2.Foundation.Runtime.Core;
using UnityEngine;

namespace PokeBlack2.Foundation.Runtime.Bootstrap
{
    [DefaultExecutionOrder(-1000)]
    public sealed class FoundationBootstrap : MonoBehaviour
    {
        [SerializeField] private GameContentProfile explicitProfile;
        [SerializeField] private string resourcesProfilePath = "Foundation/GameContentProfile";

        public GameContentProfile ActiveProfile { get; private set; }

        private void Awake()
        {
            Initialize();
        }

        public GameContentProfile Initialize()
        {
            GameContentProfile profile = explicitProfile != null
                ? explicitProfile
                : Resources.Load<GameContentProfile>(resourcesProfilePath);

            if (profile == null)
            {
                profile = GameContentProfile.CreateTransientDefault();
            }

            profile.EnsureValid();
            ActiveProfile = profile;
            return profile;
        }
    }
}

