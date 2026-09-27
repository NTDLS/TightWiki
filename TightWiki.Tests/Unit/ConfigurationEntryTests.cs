using TightWiki.Plugin.Models;

namespace TightWiki.Tests.Unit
{
    public class ConfigurationEntryTests
    {
        private static TwConfigurationEntries Entries(params (string Name, string Value)[] values)
            => new(values.Select(o => new TwConfigurationEntry { Name = o.Name, Value = o.Value }).ToList());

        [Theory]
        [InlineData("1", true)]
        [InlineData("0", false)]
        public void BooleansAreStoredAsDigits(string stored, bool expected)
        {
            //Boolean settings such as "Enable Sidebar" are persisted as "0" or "1".
            Assert.Equal(expected, Entries(("Enable Sidebar", stored)).Value("Enable Sidebar", !expected));
        }

        [Fact]
        public void MissingEntriesUseTheDefault()
        {
            //Installs that have not been upgraded yet do not have newly added entries.
            var entries = Entries(("Other", "x"));

            Assert.False(entries.Value("Enable Sidebar", false));
            Assert.Equal("Sidebar", entries.Value("Sidebar Page", "Sidebar"));
            Assert.Null(entries.Value<string>("Sidebar Page"));
        }

        [Fact]
        public void IntegersConvert()
        {
            Assert.Equal(20, Entries(("Pagination Size", "20")).Value<int>("Pagination Size"));
        }
    }
}
