using Microsoft.AspNetCore.Http;
using TightWiki.Library.Interfaces;

namespace TightWiki.Engine.Library.Interfaces
{
    public interface IWikifier
    {
        public Dictionary<string, string> Variables { get; }
        public Dictionary<string, string> Snippets { get; }
        public List<string> Tags { get; set; }
        public IPage Page { get; }
        public int? Revision { get; }
        public IQueryCollection QueryString { get; }
        public ISessionState? SessionState { get; }
        public List<string> ProcessingInstructions { get; }
        public List<NameNav> OutgoingLinks { get; }
        public string ProcessedBody { get; }
        public List<TOCTag> TableOfContents { get; }
        public int CurrentNestLevel { get; }
        public List<string> Headers { get; }

        public string GenerateQueryToken();
        public List<WeightedToken> ParsePageTokens();
    }
}
