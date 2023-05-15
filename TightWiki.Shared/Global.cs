using System.Collections.Generic;
using TightWiki.Shared.Models.Data;
using TightWiki.Shared.Repository;

namespace TightWiki.Shared
{
    public static class Global
    {
        public static List<Emoji> Emojis { get; set; } = new();

        public static void PreloadSingletons()
        {
            Emojis = ConfigurationRepository.GetAllEmojis();
        }
    }
}
