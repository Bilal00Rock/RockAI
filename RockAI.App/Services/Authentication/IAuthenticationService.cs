using System.Threading.Tasks;

namespace RockAI.App.Services.Authentication
{
    public interface IAuthenticationService
    {
        /// <summary>
        /// Attempts to authenticate with the remote API and store the received access token.
        /// Returns true on success.
        /// </summary>
        Task<bool> LoginAsync(string email, string password);

        /// <summary>
        /// Removes any locally stored token and signs the user out locally.
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// Returns the current access token (or null if none).
        /// </summary>
        Task<string?> GetAccessTokenAsync();
    }
}
