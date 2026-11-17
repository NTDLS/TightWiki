using NTDLS.SqliteDapperWrapper;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Repository.Models;

namespace TightWiki.Repository
{
    /// <summary>
    /// Stores personal API tokens in the users database. Only token hashes are stored.
    /// </summary>
    /// <remarks>
    /// This is not part of ITwUsersRepository because that interface is a public contract for plugins
    /// and its models live in TightWiki.Plugin.
    /// </remarks>
    public class ApiTokenRepository
    {
        private readonly SqliteManagedFactory _usersFactory;

        public ApiTokenRepository(ITwUsersRepository usersRepository)
        {
            _usersFactory = usersRepository.UsersFactory;
        }

        public async Task InsertApiToken(Guid userId, string name, string tokenHash)
        {
            var param = new
            {
                UserId = userId,
                Name = name,
                TokenHash = tokenHash,
                CreatedDate = DateTime.UtcNow
            };

            await _usersFactory.ExecuteAsync("InsertApiToken.sql", param);
        }

        public async Task<List<TwApiToken>> GetApiTokensByUserId(Guid userId)
            => await _usersFactory.QueryAsync<TwApiToken>("GetApiTokensByUserId.sql", new { UserId = userId });

        public async Task<Guid?> GetUserIdByApiTokenHash(string tokenHash)
            => await _usersFactory.QueryFirstOrDefaultAsync<Guid?>("GetUserIdByApiTokenHash.sql", new { TokenHash = tokenHash });

        /// <summary>
        /// Deletes the token only if it belongs to the given user.
        /// </summary>
        public async Task DeleteApiTokenByIdAndUserId(int id, Guid userId)
            => await _usersFactory.ExecuteAsync("DeleteApiTokenByIdAndUserId.sql", new { Id = id, UserId = userId });

        public async Task DeleteApiTokensByUserId(Guid userId)
            => await _usersFactory.ExecuteAsync("DeleteApiTokensByUserId.sql", new { UserId = userId });
    }
}
