using System.Reflection;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Default.StandardFunctions
{
    [TwPlugin("System & Environment Info", "Built-in standard functions.")]
    public class SystemFunctions
    {
        [TwStandardFunctionPlugin("SiteName", "Displays the title of the site.")]
        public async Task<TwPluginResult> SiteName(ITwEngineState state)
        {
            return new TwPluginResult(state.Engine.WikiConfiguration.Name)
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("DotNetVersion", "Displays the .NET version that TightWiki is running on.")]
        public async Task<TwPluginResult> DotNetVersion(ITwEngineState state)
        {
            return new TwPluginResult(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription)
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("AppVersion", "Displays the version of the wiki engine.")]
        public async Task<TwPluginResult> AppVersion(ITwEngineState state)
        {
            var version = string.Join('.', (Assembly.GetExecutingAssembly()
                .GetName().Version?.ToString() ?? "0.0.0.0").Split('.').Take(3)); //Major.Minor.Patch

            return new TwPluginResult(version)
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }
    }
}
