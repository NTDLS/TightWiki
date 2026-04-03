using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Default.StandardFunctions
{
    [TwPlugin("State, Variables & Flow", "Built-in standard functions.")]
    public class StateFunctions
    {
        [TwStandardFunctionPlugin("Set", "Sets a wiki variable.")]
        public async Task<TwPluginResult> Set(ITwEngineState state, string key, string value)
        {
            if (!state.Variables.TryAdd(key, value))
            {
                state.Variables[key] = value;
            }

            return new TwPluginResult(string.Empty)
            {
                Instructions = [TwResultInstruction.TruncateTrailingLine]
            };
        }

        [TwStandardFunctionPlugin("Get", "Gets a wiki variable.")]
        public async Task<TwPluginResult> Get(ITwEngineState state, string key)
        {
            if (state.Variables.TryGetValue(key, out var variable))
            {
                return new TwPluginResult(variable);
            }

            throw new Exception($"The wiki variable {key} is not defined. It should be set with ##Set() before calling Get().");
        }

        [TwStandardFunctionPlugin("Seq", "Inserts a sequence into the document.")]
        public async Task<TwPluginResult> Seq(ITwEngineState state, string key = "Default")
        {
            var sequences = state.GetStateValue("_sequences", new Dictionary<string, int>());

            if (sequences.ContainsKey(key) == false)
            {
                sequences.Add(key, 0);
            }

            sequences[key]++;

            return new TwPluginResult(sequences[key].ToString())
            {
                Instructions = [TwResultInstruction.OnlyReplaceFirstMatch]
            };
        }
    }
}
