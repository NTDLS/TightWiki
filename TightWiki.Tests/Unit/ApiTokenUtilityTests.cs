using TightWiki.Library.Security;

namespace TightWiki.Tests.Unit
{
    public class ApiTokenUtilityTests
    {
        [Fact]
        public void GeneratedTokenIs32BytesOfLowercaseHex()
        {
            var token = ApiTokenUtility.GenerateToken();

            Assert.Equal(64, token.Length);
            Assert.Matches("^[0-9a-f]+$", token);
        }

        [Fact]
        public void GeneratedTokensDiffer()
        {
            Assert.NotEqual(ApiTokenUtility.GenerateToken(), ApiTokenUtility.GenerateToken());
        }

        [Fact]
        public void HashIsDeterministicAndNotThePlaintext()
        {
            var token = ApiTokenUtility.GenerateToken();

            var hash = ApiTokenUtility.HashToken(token);

            Assert.Equal(hash, ApiTokenUtility.HashToken(token));
            Assert.NotEqual(token, hash);
            Assert.Equal(64, hash.Length);
        }

        [Fact]
        public void DifferentTokensHaveDifferentHashes()
        {
            Assert.NotEqual(ApiTokenUtility.HashToken("one"), ApiTokenUtility.HashToken("two"));
        }
    }
}
