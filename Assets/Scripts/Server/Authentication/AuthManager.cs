using Cysharp.Threading.Tasks;
using Server.API;
using Server.DTOs.Requests;

namespace Server.Authentication
{
    public class AuthManager
    {
        private readonly AuthApi _authApi;
        private readonly CredentialStorage _credentialStorage;

        public bool IsLoggedIn =>
            _authApi.IsLoggedIn;

        public AuthManager(
            AuthApi authApi,
            CredentialStorage credentialStorage)
        {
            _authApi = authApi;
            _credentialStorage = credentialStorage;
        }

        public async UniTask LoginAsync(
            string username,
            string password,
            bool rememberMe)
        {
            await _authApi.LoginAsync(
                new LoginRequest
                {
                    Username = username,
                    Password = password
                });

            if (rememberMe)
            {
                _credentialStorage.Save(
                    new Credential
                    {
                        Username = username,
                        Password = password
                    });
            }
            else
            {
                _credentialStorage.Clear();
            }
        }

        public Credential? LoadRememberedCredential()
        {
            return _credentialStorage.Load();
        }

        public void Logout()
        {
            _authApi.Logout();
        }

        public void ClearRememberedCredential()
        {
            _credentialStorage.Clear();
        }
    }
}