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
        private readonly TwVerbatimLocalizationText _localizer = new();

        public async Task CreateNotExisting()
        {
            foreach (var function in engine.StandardFunctions)
                await GenerateFunctionDocumentation(function);
            foreach (var function in engine.ScopeFunctions)
                await GenerateFunctionDocumentation(function);
            foreach (var function in engine.PostProcessingFunctions)
                await GenerateFunctionDocumentation(function);
            foreach (var function in engine.ProcessingFunctions)
                await GenerateFunctionDocumentation(function);
        }

        private string GetParameterTypeWikiName(Type type)
        {
            if (type.IsEnum)
            {
                type = typeof(string);
            }
            else if (type.IsArray)
            {
                type = (Nullable.GetUnderlyingType(type.GetElementType().EnsureNotNull()) ?? type.GetElementType()).EnsureNotNull();
            }
            else
            {
                type = Nullable.GetUnderlyingType(type) ?? type;
            }

            switch (type)
            {
                case var a when a == typeof(string):
                    return "string";
                case var b when b == typeof(bool):
                    return "boolean";
                case var c when c == typeof(int):
                case var d when d == typeof(long):
                case var e when e == typeof(short):
                case var f when f == typeof(byte):
                case var g when g == typeof(ulong):
                case var h when h == typeof(ushort):
                case var i when i == typeof(sbyte):
                case var j when j == typeof(uint):
                case var k when k == typeof(double):
                case var l when l == typeof(decimal):
                case var m when m == typeof(float):
                    return "numeric";
            }

            throw new Exception($"Unsupported parameter type: {type.FullName}");
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
                topic = $"Wiki Help :: {descriptor.Method.Name} Instruction";
            }

            string navigation = TwNamespaceNavigation.CleanAndValidate(topic);

            if (descriptor.Method.Name == "CreatedBy")
            {
            }

            var funcParams = descriptor.Parameters.Where(o => o.Name != "state" && o.Name != "scopeBody").ToList();

            var page = await engine.DatabaseManager.PageRepository.GetPageInfoByNavigation(navigation);
            if (page == null)
            {
                var html = new System.Text.StringBuilder();

                html.AppendLine("@@draft");
                html.AppendLine("@@HideFooterComments @@HideFooterLastModified @@protect(true)");
                html.AppendLine($"##title @@Tags(Official-Help, Help, Wiki, Official, {functionType})");
                html.AppendLine($"##toc");
                html.AppendLine("${metaColor = #ee2401}");
                html.AppendLine("${keywordColor = #318000}");
                html.AppendLine("${identifierColor = #c6680e}");
                html.AppendLine("==Overview");
                html.AppendLine($"The {descriptor.Method.Name} {functionType.ToLowerInvariant()} ... {descriptor.FunctionAttribute.Description} !!FILL_IN_THE_BLANK!!");
                html.AppendLine($"");
                html.AppendLine("");
                html.AppendLine("");
                html.AppendLine("==Prototype");
                html.Append($"##Color(${{keywordColor}}, **#{{ {(descriptor.FunctionAttribute.Demarcation == "{{" ? "" : descriptor.FunctionAttribute.Demarcation)}{descriptor.Method.Name} }}#**)");
                if (funcParams.Count == 0)
                {
                    html.AppendLine("()");
                }
                else
                {
                    html.Append('(');
                    foreach (var p in funcParams)
                    {
                        html.Append($"##Color(${{keywordColor}}, {GetParameterTypeWikiName(p.ParameterType)}{(p.ParameterType.IsArray ? ":Infinite" : "")})");
                        if (!p.HasDefaultValue)
                        {
                            html.Append($" [##Color(${{identifierColor}}, {p.Name})]");
                        }
                        else
                        {
                            html.Append($" {{##Color(${{identifierColor}}, {p.Name})}}");
                        }
                        html.Append(", ");
                    }
                    html.Length -= 2; //Remove trailing comma and space.
                    html.Append(')');
                }

                html.AppendLine("");
                html.AppendLine("");
                html.AppendLine("");
                html.AppendLine("===Parameters");
                html.AppendLine("{{Bullets");

                if (funcParams.Count == 0)
                {
                    html.AppendLine($"None.");
                }

                foreach (var p in funcParams)
                {
                    html.AppendLine($"**Name:** ##Color(${{identifierColor}}, {p.Name}) ##Color(${{metaColor}}, {(!p.HasDefaultValue ? "[Required]" : "{Optional}")})");
                    html.AppendLine($">**Type:** ##Color(${{keywordColor}}, {GetParameterTypeWikiName(p.ParameterType)}{(p.ParameterType.IsArray ? ":Infinite" : "")})");
                    if (p.HasDefaultValue)
                    {
                        if (!string.IsNullOrEmpty(p.DefaultValue?.ToString()))
                        {
                            html.AppendLine($">**Default:** ##Color(${{identifierColor}}, {p.DefaultValue})");
                        }
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
                    if (funcParams.Count == 0)
                    {
                        html.AppendLine("()");
                    }
                    else
                    {
                        html.AppendLine($"({string.Join(", ", funcParams.Select(o => o.Name))})");
                    }

                    html.AppendLine("This is the body content of the function scope.");

                    html.AppendLine("}}");
                }
                else
                {
                    html.Append($"{descriptor.FunctionAttribute.Demarcation}{descriptor.Method.Name}");
                    if (funcParams.Count == 0)
                    {
                        html.AppendLine("()");
                    }
                    else
                    {
                        html.AppendLine($"({string.Join(", ", funcParams.Select(o => o.Name))})");
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
                    Description = descriptor.FunctionAttribute.Description + "!!FILL_IN_THE_BLANK!!",
                    CreatedByUserId = adminUserId,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedByUserId = adminUserId,
                    ModifiedDate = DateTime.UtcNow,
                    Body = html.ToString()
                };

                await engine.DatabaseManager.PageRepository.UpsertPage(engine, _localizer, page);

                Console.WriteLine(html.ToString());
            }
        }
    }
}
