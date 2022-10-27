using System;
using System.Linq;
using TightWiki.Shared.Models.Data;
using TightWiki.Shared.Repository;

namespace TightWiki.Shared.Wiki.MethodCall
{
    public static class SelfDocument
    {
        public static void CreateNotExisting()
        {
            System.Threading.Thread.Sleep(500);
            foreach (var item in Shared.Wiki.MethodCall.MethodPrototypeDefinitions.Collection.Items)
            {
                string methodType = "Function";

                if (item.MethodPrefix == "##")
                    methodType = "Standard Function";
                if (item.MethodPrefix == "@@")
                    methodType = "Instruction Function";
                if (item.MethodPrefix == "$$")
                    methodType = "Scope Function";

                string topic = $"{item.ProperName} {methodType} Wiki Help";
                string navigation = WikiUtility.CleanPartialURI(topic);

                var page = PageRepository.GetPageInfoByNavigation(navigation);
                if (page == null)
                {
                    var html = new System.Text.StringBuilder();
                    html.AppendLine("@@draft");
                    html.AppendLine("@@protect(true)");
                    html.AppendLine("##Image(Wiki Help/TightWiki Logo.png | 15)");
                    html.AppendLine($"##title ##Tag(Official-Help | Help | Wiki | Official | {methodType})");
                    html.AppendLine("{{{Card(Default | Table of Contents) ##toc }}}");
                    html.AppendLine("");
                    html.AppendLine("==Overview");
                    html.AppendLine($"The {item.ProperName} {methodType.ToLower()} is !!FILL_IN_THE_BLANK!!");
                    html.AppendLine("");
                    html.AppendLine("");
                    html.AppendLine("==Prototype");
                    html.Append($"{item.ProperName}");
                    if ((item.Value.Parameters?.Count ?? 0) == 0)
                    {
                        html.AppendLine("()");
                    }
                    else
                    {
                        html.AppendLine($"({string.Join(" | ", item.Value.Parameters.Select(o => o.Name))})");
                    }

                    html.AppendLine("");
                    html.AppendLine("");
                    html.AppendLine("===Parameters");
                    html.AppendLine("{{{Bullets");

                    foreach (var p in item.Value.Parameters)
                    {
                        html.AppendLine($"**Name:** {p.Name} //({(p.IsRequired ? "Required" : "Optional")})//");
                        html.AppendLine($">**Type:** {p.Type} {(p.IsInfinite ? "//(Infinite)//" : "")}");
                        if (string.IsNullOrEmpty(p.DefaultValue) == false)
                        {
                            html.AppendLine($">**Default:** {p.DefaultValue}");
                        }
                        if (p.AllowedValues != null)
                        {
                            html.AppendLine($">**Values:** {string.Join(", ", p.AllowedValues)}");
                        }
                        html.AppendLine($">**Description:** !!FILL_IN_THE_BLANK!!");
                    }
                    html.AppendLine("}}}");
                    html.AppendLine("");
                    html.AppendLine("");

                    html.AppendLine("==Examples");
                    html.AppendLine("{{{Card [{{");

                    if (item.MethodPrefix == "$$")
                    {
                        html.Append("{{{ " + $"{item.ProperName}");
                        if ((item.Value.Parameters?.Count ?? 0) == 0)
                        {
                            html.AppendLine("()");
                        }
                        else
                        {
                            html.AppendLine($"({string.Join(" | ", item.Value.Parameters.Select(o => o.Name))})");
                        }

                        html.AppendLine("This is the body content of the function scope.");

                        html.AppendLine("}}}");
                    }
                    else
                    {
                        html.Append($"{item.MethodPrefix}{item.ProperName}");
                        if ((item.Value.Parameters?.Count ?? 0) == 0)
                        {
                            html.AppendLine("()");
                        }
                        else
                        {
                            html.AppendLine($"({string.Join(" | ", item.Value.Parameters.Select(o => o.Name))})");
                        }
                    }

                    html.AppendLine("}}]}}}");
                    html.AppendLine("");

                    html.AppendLine("==See Also");
                    html.AppendLine("[[Function Calling Convention Wiki Help]]");
                    html.AppendLine("[[Scope Function Wiki Help]]");
                    html.AppendLine("[[Instruction Function Wiki Help]]");
                    html.AppendLine("[[Standard Function Wiki Help]]");
                    html.AppendLine("");
                    html.AppendLine("");

                    html.AppendLine("==Related");
                    html.AppendLine("##related");

                    page = new Page()
                    {
                        Navigation = navigation,
                        Name = topic,
                        Description = $"Documentation of the built-in {item.ProperName.ToLower()} {methodType.ToLower()}.",
                        CreatedByUserId = 1,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedByUserId = 1,
                        ModifiedDate = DateTime.UtcNow,
                        Body = html.ToString()
                    };

                    PageRepository.SavePage(page);
                }
            }
        }
    }
}
