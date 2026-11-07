using TightWiki.Library;
using TightWiki.Library.Security;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// The database upgrade scripts only run when the version changes, so the ApiTokens table is
    /// created here by running the upgrade script itself against the copy of the users database.
    /// </summary>
    [Collection("Markup Tests")]
    public class ApiTokenRepositoryTests
        : IClassFixture<TwEngineFixture>
    {
        private readonly TwEngineFixture _fixture;

        public ApiTokenRepositoryTests(TwEngineFixture fixture)
        {
            _fixture = fixture;

            var usersFactory = _fixture.Artifacts.DatabaseManager.UsersRepository.UsersFactory;
            if (!usersFactory.DoesTableExist("ApiTokens"))
            {
                //The first line of the script is the --##IF macro, which is a plain SQL comment when executed directly.
                //The version folder is not part of the lookup because the build prefixes folder names that start with a digit.
                var script = EmbeddedResourceReader.LoadText("^001^Users^ApiTokens.sql");
                usersFactory.Execute(script);
            }
        }

        private async Task<string> CreateToken(Guid userId, string name)
        {
            var token = ApiTokenUtility.GenerateToken();
            await _fixture.Artifacts.DatabaseManager.ApiTokenRepository.InsertApiToken(userId, name, ApiTokenUtility.HashToken(token));
            return token;
        }

        [Fact]
        public async Task CreatedTokenResolvesToItsUserByHash()
        {
            var repository = _fixture.Artifacts.DatabaseManager.ApiTokenRepository;
            var userId = Guid.NewGuid();

            var token = await CreateToken(userId, "Created token");

            var foundUserId = await repository.GetUserIdByApiTokenHash(ApiTokenUtility.HashToken(token));
            Assert.Equal(userId, foundUserId);
        }

        [Fact]
        public async Task UnknownTokenResolvesToNoUser()
        {
            var repository = _fixture.Artifacts.DatabaseManager.ApiTokenRepository;

            var foundUserId = await repository.GetUserIdByApiTokenHash(ApiTokenUtility.HashToken(ApiTokenUtility.GenerateToken()));

            Assert.Null(foundUserId);
        }

        [Fact]
        public async Task UserSeesOnlyOwnTokens()
        {
            var repository = _fixture.Artifacts.DatabaseManager.ApiTokenRepository;
            var userId = Guid.NewGuid();
            await CreateToken(userId, "Mine");
            await CreateToken(Guid.NewGuid(), "Someone else's");

            var tokens = await repository.GetApiTokensByUserId(userId);

            var token = Assert.Single(tokens);
            Assert.Equal("Mine", token.Name);
            Assert.True(token.CreatedDate > DateTime.UtcNow.AddMinutes(-5));
        }

        [Fact]
        public async Task RevokeWithAnotherUsersIdDeletesNothing()
        {
            var repository = _fixture.Artifacts.DatabaseManager.ApiTokenRepository;
            var ownerId = Guid.NewGuid();
            var token = await CreateToken(ownerId, "Owned");
            var tokenId = (await repository.GetApiTokensByUserId(ownerId)).Single().Id;

            await repository.DeleteApiTokenByIdAndUserId(tokenId, Guid.NewGuid());

            Assert.Equal(ownerId, await repository.GetUserIdByApiTokenHash(ApiTokenUtility.HashToken(token)));
            Assert.Single(await repository.GetApiTokensByUserId(ownerId));
        }

        [Fact]
        public async Task RevokeWithOwnerIdDeletesTheToken()
        {
            var repository = _fixture.Artifacts.DatabaseManager.ApiTokenRepository;
            var ownerId = Guid.NewGuid();
            var token = await CreateToken(ownerId, "Owned");
            var tokenId = (await repository.GetApiTokensByUserId(ownerId)).Single().Id;

            await repository.DeleteApiTokenByIdAndUserId(tokenId, ownerId);

            Assert.Null(await repository.GetUserIdByApiTokenHash(ApiTokenUtility.HashToken(token)));
            Assert.Empty(await repository.GetApiTokensByUserId(ownerId));
        }

        [Fact]
        public async Task DeleteAllRemovesOnlyThatUsersTokens()
        {
            var repository = _fixture.Artifacts.DatabaseManager.ApiTokenRepository;
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            await CreateToken(userId, "First");
            await CreateToken(userId, "Second");
            await CreateToken(otherUserId, "Other");

            await repository.DeleteApiTokensByUserId(userId);

            Assert.Empty(await repository.GetApiTokensByUserId(userId));
            Assert.Single(await repository.GetApiTokensByUserId(otherUserId));
        }
    }
}
