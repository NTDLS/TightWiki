using System.Text;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Default.StandardFunctions
{
    [TwPlugin("Emoji System", "Built-in standard functions.")]
    public class EmojiFunctions
    {
        [TwStandardFunctionPlugin("SystemEmojiList", "Displays a list of emojis for the specified category.")]
        public async Task<TwPluginResult> SystemEmojiList(ITwEngineState state)
        {
            StringBuilder html = new();

            html.Append($"<table class=\"table table-striped table-bordered \">");

            html.Append($"<thead>");
            html.Append($"<tr>");
            html.Append($"<td><strong>Name</strong></td>");
            html.Append($"<td><strong>Image</strong></td>");
            html.Append($"<td><strong>Shortcut</strong></td>");
            html.Append($"</tr>");
            html.Append($"</thead>");

            string category = state.QueryString["Category"].ToString();

            html.Append($"<tbody>");

            if (string.IsNullOrWhiteSpace(category) == false)
            {
                var emojis = await state.Engine.DatabaseManager.EmojiRepository.GetEmojisByCategory(category);

                foreach (var emoji in emojis)
                {
                    html.Append($"<tr>");
                    html.Append($"<td>{emoji.Name}</td>");
                    //html.Append($"<td><img src=\"/images/emoji/{emoji.Path}\" /></td>");
                    html.Append($"<td><img src=\"{state.Engine.WikiConfiguration.BasePath}/File/Emoji/{emoji.Name.ToLowerInvariant()}\" /></td>");
                    html.Append($"<td>{emoji.Shortcut}</td>");
                    html.Append($"</tr>");
                }
            }

            html.Append($"</tbody>");
            html.Append($"</table>");

            return new TwPluginResult(html.ToString())
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("SystemEmojiCategoryList", "Displays a list of emoji categories.")]
        public async Task<TwPluginResult> SystemEmojiCategoryList(ITwEngineState state)
        {
            var categories = await state.Engine.DatabaseManager.EmojiRepository.GetEmojiCategoriesGrouped();

            StringBuilder html = new();

            html.Append($"<table class=\"table table-striped table-bordered \">");

            int rowNumber = 0;

            html.Append($"<thead>");
            html.Append($"<tr>");
            html.Append($"<td><strong>Name</strong></td>");
            html.Append($"<td><strong>Count of Emojis</strong></td>");
            html.Append($"</tr>");
            html.Append($"</thead>");

            foreach (var category in categories)
            {
                if (rowNumber == 1)
                {
                    html.Append($"<tbody>");
                }

                html.Append($"<tr>");
                html.Append($"<td><a href=\"{state.Engine.WikiConfiguration.BasePath}/wiki_help::list_of_emojis_by_category?category={category.Category}\">{category.Category}</a></td>");
                html.Append($"<td>{category.EmojiCount:N0}</td>");
                html.Append($"</tr>");
                rowNumber++;
            }

            html.Append($"</tbody>");
            html.Append($"</table>");

            return new TwPluginResult(html.ToString())
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }
    }
}
