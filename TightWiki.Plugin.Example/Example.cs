using System.Text;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Plugin.Example
{
    public class Example
    {
        [TwPlugin("My Custom Plugin", "Custom functions written by me.", 1)]
        public class MyCustomPlugin
        {
            [TwStandardFunctionPlugin("Example hello function", "Writes hello to the page .", 1)]
            public async Task<TwPluginResult> HelloWorld(ITwEngineState state, int count)
            {
                var stringBuilder = new StringBuilder();

                for (int i = 0; i < count; i++)
                {
                    //Note that here we are generating a string that contains markup,
                    //  that markup will also be processed by the engine.
                    stringBuilder.AppendLine($"**Hello World** {i + 1}!");
                }

                return new TwPluginResult(stringBuilder.ToString())
                {
                    Instructions = [
                        //Examples of some of the common instructions we can supply along with the generated text:

                        //TwResultInstruction.Skip, //Does not process the match, allowing it to be processed by another handler.
                        //TwResultInstruction.TruncateTrailingLine, // Removes any single trailing newline after match.
                        //TwResultInstruction.DisallowNestedProcessing, // Will not continue to process content in this block.
                    ]
                };
            }

            [TwScopeFunctionPlugin("Example hello scope function", "Places a hello world tag at the top and bottom of your scope.", 1)]
            public async Task<TwPluginResult> HelloWorld(ITwEngineState state, string scopeBody)
            {
                return new TwPluginResult($"**BEGIN HELLO WORLD->** {scopeBody} **<-END HELLO WORLD**");
            }
        }
    }
}
