# TightWiki.Plugin
TightWiki.Plugin is a package that provides boilerplate artifacts for the TightWiki
plugins.

It allows developers to create and integrate custom plugins to extend the functionality of TightWiki.

## Installation
Plugins built (the .DLL) needs to be placed into the `Plugins` folder of the TightWiki installation directory. After placing the plugin, it will be automatically loaded by TightWiki when the site is next started.

## Usage
Decorate your class with the `TwPlugin` attribute to define a plugin, and decorate your methods with the one of the TightWiki function attributes to define functions that can be called from TightWiki pages.

```
[TwStandardFunctionPlugin("DotNetVersion", "Displays the .NET version that TightWiki is running on.")]
public async Task<TwPluginResult> DotNetVersion(ITwEngineState state)
{
    return new TwPluginResult(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription)
    {
        Instructions = [TwResultInstruction.DisallowNestedProcessing]
    };
}

[TwScopeFunctionPlugin("Alert", "Renders an alert box with optional style and title.")]
public async Task<TwPluginResult> Alert(ITwEngineState state, string scopeBody,
    TwBootstrapStyle styleName = TwBootstrapStyle.Default, string titleText = "")
{
    var html = new StringBuilder();

    var style = styleName == TwBootstrapStyle.Default ? "" : $"alert-{styleName.ToString().ToLowerInvariant()}";

    if (!string.IsNullOrEmpty(titleText)) scopeBody = $"<h3>{titleText}</h3>{scopeBody}";
    html.Append($"<div class=\"alert {style} shadow-lg\">{scopeBody}</div>");
    return new TwPluginResult(html.ToString());
}

[TwMarkupPluginHandler("Default markup handler",
    "Handles basic markup instructions like bold, italic, underline, etc.")]
[TwPluginRegularExpression(@"\~\~(.*?)\~\~")]
[TwPluginRegularExpression(@"\*\*(.*?)\*\*")]
[TwPluginRegularExpression(@"__(.*?)__")]
[TwPluginRegularExpression(@"\/\/(.*?)\/\/")]
[TwPluginRegularExpression(@"\!\!(.*?)\!\!")]
public async Task<TwPluginResult> HandleMarkup(ITwEngineState state, TwOrderedMatch match)
{
    char sequence = match.Value[0];
    string body = match.Value.Substring(2, match.Value.Length - 4);

    switch (sequence)
    {
        case '~': return new TwPluginResult($"<strike>{body}</strike>");
        case '*': return new TwPluginResult($"<strong>{body}</strong>");
        case '_': return new TwPluginResult($"<u>{body}</u>");
        case '/': return new TwPluginResult($"<i>{body}</i>");
        case '!': return new TwPluginResult($"<mark>{body}</mark>");
        default:
            break;
    }

    return new TwPluginResult() { Instructions = [TwResultInstruction.Skip] };
}

```

## Method Attributes
### Functions
- __TwPostProcessingInstructionFunctionPlugin__
  - Function called when a post-processing instruction has been parsed in the wiki markup.
- __TwProcessingInstructionFunctionPlugin__
  - Function called when a processing instruction has been parsed in the wiki markup.
- __TwScopeFunctionPlugin__
  - Function called when a scope function has been parsed in the wiki markup.
- __TwStandardFunctionPlugin__
 - Function called when a standard function has been parsed in the wiki markup.

### Handlers
- __TwCompletionPluginHandler__
  - Function called when the wiki engine has completed processing a page.
- __TwExceptionPluginHandler__
  - Function called when an exception has been encountered during processing.
- __TwMarkupPluginHandler__
  - Function to handle custom wiki markup. The function will be called when the wiki engine encounters markup specified by the TwPluginRegularExpression attribute.

## Contributing
Contributions to TightWiki.Plugin are welcome! If you have an idea for a new feature or have found a bug, please open an issue or submit a pull request on the GitHub repository.

## License
[MIT](https://choosealicense.com/licenses/mit/)
