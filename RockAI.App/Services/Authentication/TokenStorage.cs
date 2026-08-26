using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace RockAI.App.Services.Authentication
{
    public interface ITokenStorage
    {
        Task SetTokenAsync(string token);
        Task<string?> GetTokenAsync();
        Task RemoveTokenAsync();
    }

    public class SecureTokenStorage : ITokenStorage
    {
        private const string TokenKey = "access_token";

        public Task SetTokenAsync(string token) =>
            SecureStorage.Default.SetAsync(TokenKey, token);

        public async Task<string?> GetTokenAsync()
        {
            try
            {
                return await SecureStorage.Default.GetAsync(TokenKey);
            }
            catch (Exception ex) when (
                ex is ArgumentException
                || ex is PlatformNotSupportedException
                || ex is NotSupportedException
                || ex is System.Security.Cryptography.CryptographicException
                || ex is UnauthorizedAccessException)
            {
                // Expected secure-storage related failures: treat as missing token.
                return null;
            }
        }

        public Task RemoveTokenAsync()
        {
            // Some MAUI platforms expose Remove instead of RemoveAsync; call Remove when available
            SecureStorage.Default.Remove(TokenKey);
            return Task.CompletedTask;
        }
    }
}
