using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Models;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Plugin.Default.StandardFunctions
{
    [TwPlugin("Page Metadata", "Built-in standard functions.")]
    public class MetadataFunctions
    {
        [TwStandardFunctionPlugin("LastModifiedBy", "Displays the name of the person last modified the current page.")]
        public async Task<TwPluginResult> LastModifiedBy(ITwEngineState state)
        {
            return new TwPluginResult(state.Page.ModifiedByUserName)
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("CreatedBy", "Displays the name of the person who created the current page.")]
        public async Task<TwPluginResult> CreatedBy(ITwEngineState state)
        {
            return new TwPluginResult(state.Page.CreatedByUserName)
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("PageRevisionCount", "Displays the total number of revisions for the current page.")]
        public async Task<TwPluginResult> PageRevisionCount(ITwEngineState state)
        {
            return new TwPluginResult($"{state.Page.MostCurrentRevision:n0}")
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }


        [TwStandardFunctionPlugin("PageviewCount", "Displays the total number views for the current page.")]
        public async Task<TwPluginResult> PageviewCount(ITwEngineState state)
        {
            int totalPageCount = await state.Engine.DatabaseManager.StatisticsRepository.GetPageTotalViewCount(state.Page.Id);
            return new TwPluginResult($"{totalPageCount:n0}")
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("PageCommentCount", "Displays the total number of comments for the current page.")]
        public async Task<TwPluginResult> PageCommentCount(ITwEngineState state)
        {
            int totalCommentCount = await state.Engine.DatabaseManager.PageRepository.GetTotalPageCommentCount(state.Page.Id);
            return new TwPluginResult($"{totalCommentCount:n0}")
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("PageURL", "Displays the URL for the current page.")]
        public async Task<TwPluginResult> PageURL(ITwEngineState state, TwLinkStyle styleName = TwLinkStyle.Link)
        {

            var siteAddress = (await state.Engine.DatabaseManager.ConfigurationRepository.Get(TwConfigGroup.Basic, "Address", "http://localhost")).TrimEnd('/');
            var link = $"{siteAddress}/{state.Page.Navigation}";

            switch (styleName)
            {
                case TwLinkStyle.Text:
                    return new TwPluginResult(link)
                    {
                        Instructions = [TwResultInstruction.DisallowNestedProcessing]
                    };
                case TwLinkStyle.Link:
                    return new TwPluginResult($"<a href='{link}'>{siteAddress}/{state.Page.Name}</a>")
                    {
                        Instructions = [TwResultInstruction.DisallowNestedProcessing]
                    };
                case TwLinkStyle.LinkName:
                    return new TwPluginResult($"<a href='{link}'>{state.Page.Name}</a>")
                    {
                        Instructions = [TwResultInstruction.DisallowNestedProcessing]
                    };
            }

            return new TwPluginResult();
        }

        [TwStandardFunctionPlugin("PageId", "Displays the ID of the current page.")]
        public async Task<TwPluginResult> PageId(ITwEngineState state)
        {
            return new TwPluginResult($"{state.Page.Id}")
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }


        [TwStandardFunctionPlugin("LastModified", "Displays the date and time that the current page was last modified.")]
        public async Task<TwPluginResult> LastModified(ITwEngineState state)
        {
            if (state.Session == null)
            {
                throw new Exception($"Localization is not supported without SessionState.");
            }

            if (state.Page.ModifiedDate != DateTime.MinValue)
            {
                var localized = state.Session.LocalizeDateTime(state.Page.ModifiedDate);
                return new TwPluginResult($"{localized.ToShortDateString()} {localized.ToShortTimeString()}")
                {
                    Instructions = [TwResultInstruction.DisallowNestedProcessing]
                };
            }

            return new TwPluginResult(string.Empty);
        }

        [TwStandardFunctionPlugin("Created", "Displays the date and time that the current page was created.")]
        public async Task<TwPluginResult> Created(ITwEngineState state)
        {
            if (state.Session == null)
            {
                throw new Exception($"Localization is not supported without SessionState.");
            }

            if (state.Page.CreatedDate != DateTime.MinValue)
            {
                var localized = state.Session.LocalizeDateTime(state.Page.CreatedDate);
                return new TwPluginResult($"{localized.ToShortDateString()} {localized.ToShortTimeString()}")
                {
                    Instructions = [TwResultInstruction.DisallowNestedProcessing]
                };
            }

            return new TwPluginResult(string.Empty);
        }

        [TwStandardFunctionPlugin("Name", "Displays the title of the current page.")]
        public async Task<TwPluginResult> Name(ITwEngineState state)
        {
            return new TwPluginResult(state.Page.Title)
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("Title", "Displays the title of the current page in title form.")]
        public async Task<TwPluginResult> Title(ITwEngineState state)
        {
            return new TwPluginResult($"<h1>{state.Page.Title}</h1>")
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }


        [TwStandardFunctionPlugin("Description", "Displays the description of the current page.")]
        public async Task<TwPluginResult> Description(ITwEngineState state)
        {
            return new TwPluginResult($"{state.Page.Description}")
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("Namespace", "Displays the namespace of the current page.")]
        public async Task<TwPluginResult> @Namespace(ITwEngineState state)
        {
            return new TwPluginResult(state.Page.Namespace ?? string.Empty)
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }
    }
}
