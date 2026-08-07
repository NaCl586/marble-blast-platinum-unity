using UnityEngine;
using Server.API;
using Server.Authentication;
using Server.Config;
using Server.Replay;

namespace Server
{
    public class OnlineManager : MonoBehaviour
    {
        public static OnlineManager Instance
        {
            get;
            private set;
        }

        [Header("Configuration")]
        [SerializeField]
        private ServerConfig serverConfig;

        public AuthManager Auth
        {
            get;
            private set;
        }

        public ScoreApi Score
        {
            get;
            private set;
        }

        public ReplayUploadManager Replay
        {
            get;
            private set;
        }

        public LeaderboardApi Leaderboard
        {
            get;
            private set;
        }

        private ApiClient _apiClient;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            DontDestroyOnLoad(gameObject);

            InitializeServices();
        }

        private void InitializeServices()
        {
            // Core

            _apiClient =
                new ApiClient(serverConfig);

            // APIs

            AuthApi authApi =
                new AuthApi(_apiClient);

            Score =
                new ScoreApi(_apiClient);

            ReplayApi replayApi =
                new ReplayApi(_apiClient);

            Leaderboard =
                new LeaderboardApi(_apiClient);

            // Storage

            CredentialStorage credentialStorage =
                new CredentialStorage();

            ReplayQueue replayQueue =
                new ReplayQueue();

            // Managers

            Replay =
                new ReplayUploadManager(
                    replayApi,
                    replayQueue);

            Auth =
                new AuthManager(
                    authApi,
                    credentialStorage);
        }
    }
}