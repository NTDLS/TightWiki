using NTDLS.Helpers;
using TightWiki.Library.Dummy;
using TightWiki.Plugin;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Module.Function;
using TightWiki.Plugin.Models;

namespace TightWiki.Library
{
    /// <summary>
    /// Don't ever look at this. :(
    /// </summary>
    public class SelfDocument(ITwEngine engine)
    {
        private readonly TwVerbatimLocalizationText _localizer = new ();

        public async Task CreateNotExisting()
        {
            foreach (var function in engine.StandardFunctions)
            {
                await GenerateFunctionDocumentation(function);
            }
        }

        private async Task GenerateFunctionDocumentation(ITwFunctionDescriptor descriptor)
        {
            var adminUserId = (await engine.DatabaseManager.UsersRepository.GetUserAccountIdByNavigation("admin")).EnsureNotNull();

            string functionType = "Function";

            if (descriptor.FunctionAttribute is TwPostProcessingInstructionFunctionPluginAttribute)
            {
                functionType = "Instruction Function";
            }
            else if (descriptor.FunctionAttribute is TwProcessingInstructionFunctionPluginAttribute)
            {
                functionType = "Instruction Function";
            }
            else if (descriptor.FunctionAttribute is TwScopeFunctionPluginAttribute)
            {
                functionType = "Scope Function";
            }
            else if (descriptor.FunctionAttribute is TwStandardFunctionPluginAttribute)
            {
                functionType = "Standard Function";
            }

            string topic = $"Wiki Help :: {descriptor.Method.Name}";

            if (functionType == "Instruction Function")
            {
                topic = $"Wiki Help :: {descriptor.Method.Name} - Instruction";
            }

            string navigation = TwNamespaceNavigation.CleanAndValidate(topic);

            var page = await engine.DatabaseManager.PageRepository.GetPageInfoByNavigation(navigation);
            if (page == null)
            {
                var html = new System.Text.StringBuilder();
                html.AppendLine("@@draft");
                html.AppendLine("@@protect(true)");
                html.AppendLine("##Image(Wiki Help :: Wiki Help/TightWiki Logo.png, 15)");
                html.AppendLine($"##title ##Tag(Official-Help, Help, Wiki, Official, {functionType})");
                html.AppendLine("{{Card(Default, Table of Contents) ##toc }}");
                html.AppendLine("");
                html.AppendLine("${metaColor = #ee2401}");
                html.AppendLine("${keywordColor = #318000}");
                html.AppendLine("${identifierColor = #c6680e}");
                html.AppendLine("==Overview");
                html.AppendLine($"The {descriptor.Method.Name} {functionType.ToLowerInvariant()} is !!FILL_IN_THE_BLANK!!");
                html.AppendLine("");
                html.AppendLine("");
                html.AppendLine("==Prototype");
                html.Append($"##Color(${{keywordColor}}, **#{{ {descriptor.FunctionAttribute.Demarcation}{descriptor.Method.Name} }}#**)");
                if (descriptor.Parameters.Count == 0)
                {
                    html.AppendLine("()");
                }
                else
                {
                    html.Append('(');
                    foreach (var p in descriptor.Parameters)
                    {
                        html.Append($"##Color(${{keywordColor}}, {p.ParameterType.ToString()}{(p.ParameterType.IsArray ? ":Infinite" : "")})");
                        if (p.HasDefaultValue)
                        {
                            html.Append($" [##Color(${{identifierColor}}, {p.Name})]");
                        }
                        else
                        {
                            html.Append($" {{##Color(${{identifierColor}}, {p.Name})}}");
                        }
                        html.Append(", ");
                    }
                    html.Length -= 3;
                    html.Append(')');
                }

                html.AppendLine("");
                html.AppendLine("");
                html.AppendLine("");
                html.AppendLine("===Parameters");
                html.AppendLine("{{Bullets");

                if (descriptor.Parameters.Count == 0)
                {
                    html.AppendLine($"None.");
                }

                foreach (var p in descriptor.Parameters)
                {
                    html.AppendLine($"**Name:** ##Color(${{identifierColor}}, {p.Name}) ##Color(${{metaColor}}, {(p.HasDefaultValue ? "[Required]" : "{Optional}")})");
                    html.AppendLine($">**Type:** ##Color(${{keywordColor}}, {p.ParameterType.ToString()}{(p.ParameterType.IsArray ? ":Infinite" : "")})");
                    if (p.HasDefaultValue)
                    {
                        html.AppendLine($">**Default:** ##Color(${{identifierColor}}, {p.DefaultValue})");
                    }
                    if (p.ParameterType.IsEnum)
                    {
                        var enumValues = Enum.GetValues(p.ParameterType).Cast<Enum>().Select(x => x.ToString());

                        html.AppendLine($">**Values:** ##Color(${{identifierColor}}, \"{string.Join(", ", enumValues)}\")");
                    }
                    html.AppendLine($">**Description:** !!FILL_IN_THE_BLANK!!");
                }
                html.AppendLine("}}");
                html.AppendLine("");

                html.AppendLine("==Examples");
                html.AppendLine("{{Code(wiki)#{");


                if (descriptor.FunctionAttribute.Demarcation == "$$")
                {
                    html.Append("{{ " + $"{descriptor.Method.Name}");
                    if (descriptor.Parameters.Count == 0)
                    {
                        html.AppendLine("()");
                    }
                    else
                    {
                        html.AppendLine($"({string.Join(", ", descriptor.Parameters.Select(o => o.Name))})");
                    }

                    html.AppendLine("This is the body content of the function scope.");

                    html.AppendLine("}}");
                }
                else
                {
                    html.Append($"{descriptor.FunctionAttribute.Demarcation}{descriptor.Method.Name}");
                    if (descriptor.Parameters.Count == 0)
                    {
                        html.AppendLine("()");
                    }
                    else
                    {
                        html.AppendLine($"({string.Join(", ", descriptor.Parameters.Select(o => o.Name))})");
                    }
                }

                html.AppendLine("}#}}");
                html.AppendLine("");

                html.AppendLine("==See Also");
                html.AppendLine("[[Wiki Help :: Function Calling Convention]]");
                html.AppendLine("[[Wiki Help :: Scope Function]]");
                html.AppendLine("[[Wiki Help :: Instruction Function]]");
                html.AppendLine("[[Wiki Help :: Standard Function]]");
                html.AppendLine("");
                html.AppendLine("");

                html.AppendLine("==Related");
                html.AppendLine("##related");

                page = new TwPage()
                {
                    Navigation = navigation,
                    Name = topic,
                    Description = $"Documentation of the built-in {descriptor.Method.Name.ToLowerInvariant()} {functionType.ToLowerInvariant()} !!FILL_IN_THE_BLANK!!.",
                    CreatedByUserId = adminUserId,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedByUserId = adminUserId,
                    ModifiedDate = DateTime.UtcNow,
                    Body = html.ToString()
                };

                await engine.DatabaseManager.PageRepository.UpsertPage(engine, _localizer, page);

                Console.WriteLine(html.ToString());
                Console.ReadLine();
            }
        }
    }
}
