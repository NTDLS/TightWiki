using NTDLS.Helpers;
using System.Text;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Library;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Default.StandardFunctions
{
    [TwPlugin("User & Profile", "Built-in standard functions.")]
    public class UsersFunctions
    {
        [TwStandardFunctionPlugin("ProfileGlossary", "Creates a glossary of all user profiles.")]
        public async Task<TwPluginResult> ProfileGlossary(ITwEngineState state, int pageSize = 100, string? searchToken = null)
        {
            if (!state.Engine.WikiConfiguration.EnablePublicProfiles)
            {
                return new TwPluginResult("Public profiles are disabled.");
            }

            var html = new StringBuilder();
            string refTag = state.GetNextTagMarker("ProfileGlossary");
            int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
            var profiles = await state.Engine.DatabaseManager.UsersRepository.GetAllPublicProfilesPaged(pageNumber, pageSize, searchToken);

            string glossaryName = "glossary_" + new Random().Next(0000).ToString();
            var alphabet = profiles.Select(p => p.AccountName.Substring(0, 1).ToUpperInvariant()).Distinct();

            if (profiles.Count > 0)
            {
                html.Append("<center>");
                foreach (var alpha in alphabet)
                {
                    html.Append("<a href=\"#" + glossaryName + "_" + alpha + "\">" + alpha + "</a>&nbsp;");
                }
                html.Append("</center>");

                html.Append("<ul>");
                foreach (var alpha in alphabet)
                {
                    html.Append("<li><a name=\"" + glossaryName + "_" + alpha + "\">" + alpha + "</a></li>");

                    html.Append("<ul>");
                    foreach (var profile in profiles.Where(p => p.AccountName.StartsWith(alpha, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/Profile/{profile.Navigation}/Public\">{profile.AccountName}</a>");
                        html.Append("</li>");
                    }
                    html.Append("</ul>");
                }

                html.Append("</ul>");
            }
            return new TwPluginResult(html.ToString());
        }

        //Creates a list of all user profiles.
        [TwStandardFunctionPlugin("ProfileList", "Creates a list of all user profiles.")]
        public async Task<TwPluginResult> ProfileList(ITwEngineState state, int pageSize = 100, string? searchToken = null)
        {
            if (!state.Engine.WikiConfiguration.EnablePublicProfiles)
            {
                return new TwPluginResult("Public profiles are disabled.");
            }

            var html = new StringBuilder();
            string refTag = state.GetNextTagMarker("ProfileList");
            int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
            var profiles = await state.Engine.DatabaseManager.UsersRepository.GetAllPublicProfilesPaged(pageNumber, pageSize, searchToken);
            html.Append($"<div id=\"{refTag}\"></div>");

            if (profiles.Count > 0)
            {
                html.Append("<ul>");

                foreach (var profile in profiles)
                {
                    html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/Profile/{profile.Navigation}/Public\">{profile.AccountName}</a>");
                    html.Append("</li>");
                }

                html.Append("</ul>");
            }

            if (profiles.Count > 0 && profiles.First().PaginationPageCount > 1)
            {
                html.Append(TwPageSelectorGenerator.Generate(state.QueryString, profiles.First().PaginationPageCount, refTag));
            }

            return new TwPluginResult(html.ToString());
        }
    }
}
