using System.Text;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Attributes.Handlers;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Plugin.Example
{
    public class Example
    {
        [TwPlugin("My Custom Plugin", "Custom functions written by me.", 0)]
        public class MyCustomPlugin
        {
            /// <summary>
            /// Generates a formatted string containing multiple 'Hello World' messages and returns it as a plugin
            /// result.
            /// </summary>
            /// <remarks>The returned result may include additional instructions that affect how the
            /// engine processes the generated content, such as skipping further processing or modifying trailing
            /// newlines.</remarks>
            /// <param name="state">The current engine state used to provide context for the operation.</param>
            /// <param name="count">The number of 'Hello World' messages to generate. Must be zero or greater.</param>
            /// <returns>A plugin result containing the generated 'Hello World' messages as formatted text.</returns>
            [TwStandardFunctionPlugin("Example hello function", "Writes hello to the page.", 0)]
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

            /// <summary>
            /// 
            /// </summary>
            /// <param name="state"></param>
            /// <param name="scopeBody"></param>
            /// <returns></returns>
            [TwScopeFunctionPlugin("Example hello scope function", "Places a hello world tag at the top and bottom of your scope.", 0)]
            public async Task<TwPluginResult> HelloWorld(ITwEngineState state, string scopeBody)
            {
                return new TwPluginResult($"**BEGIN HELLO WORLD->** {scopeBody} **<-END HELLO WORLD**");
            }

            /// <summary>
            /// Handles basic markup instructions like bold, italic, underline, etc.
            /// </summary>
            /// <param name="state">Reference to the wiki state object</param>
            /// <param name="match">The matched string from the regular expression</param>
            [TwMarkupPluginHandler("Custom markup handler",
                "Handles custome markup instructions.",
                precedence: 70, isLitePermissiable: true)]
            [TwPluginRegularExpression(@"\&\&(.*?)\&\&")]
            [TwPluginRegularExpression(@"\$\$(.*?)\$\$")]
            public async Task<TwPluginResult> HandleMarkup(ITwEngineState state, TwOrderedMatch match)
            {
                char sequence = match.Value[0];
                string body = match.Value.Substring(2, match.Value.Length - 4);

                switch (sequence)
                {
                    case '&': return new TwPluginResult($"<strike>{body}</strike>");
                    case '$': return new TwPluginResult($"<strong>{body}</strong>");
                    default:
                        break;
                }

                return new TwPluginResult() { Instructions = [TwResultInstruction.Skip] };
            }
        }
    }
}
