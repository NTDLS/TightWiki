# TightWiki.Plugin
TightWiki.Plugin is a package that provides boilerplate artifacts for the TightWiki
plugins.

It allows developers to create and integrate custom plugins to extend the functionality of TightWiki.

## Installation
Plugins built (the .DLL) needs to be placed into the `Plugins` folder of the TightWiki installation directory. After placing the plugin, it will be automatically loaded by TightWiki when the site is next started.

## Usage
Decorate your class with the `TwPlugin` attribute to define a plugin, and decorate your methods with the one of the TightWiki function attributes to define functions that can be called from TightWiki pages.

```
[TwPlugin("My Custom Plugin", "Custom functions written by me.", 1)]
public class MyCustomPlugin
{
    [TwStandardFunctionPlugin("Hello function", "Writes hello to the page.", 1)]
    public async Task<TwPluginResult> HelloWorld(ITwEngineState state, int count)
    {
        var stringBuilder = new StringBuilder();

        for(int i = 0; i < count; i++)
        {
            stringBuilder.AppendLine($"Hello World {i + 1}!");
        }

        return new TwPluginResult(stringBuilder.ToString());
    }
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
- __TwCommentPluginHandler__
  - Function called when a comment has been parsed in the wiki markup.
- __TwCompletionPluginHandler__
  - Function called when the wiki engine has completed processing a page.
- __TwEmojiPluginHandler__
  - Function called when an emoji has been parsed in the wiki markup.
- __TwExceptionPluginHandler__
  - Function called when an exception has been encountered during processing.
- __TwExternalLinkPluginHandler__
  - Function called when an external link has been parsed in the wiki markup.
- __TwHeadingPluginHandler__
  - Function called when a heading has been parsed in the wiki markup.
- __TwInternalLinkPluginHandler__
  - Function called when an internal link has been parsed in the wiki markup.
- __TwMarkupPluginHandler__
  - Function called when a markup element has been parsed in the wiki markup.

## Contributing
Contributions to TightWiki.Plugin are welcome! If you have an idea for a new feature or have found a bug, please open an issue or submit a pull request on the GitHub repository.

## License
[MIT](https://choosealicense.com/licenses/mit/)
