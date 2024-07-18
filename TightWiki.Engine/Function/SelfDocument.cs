namespace TightWiki.Wiki.Function
{
    public static class SelfDocument
    {
        /// <summary>
        /// Don't ever look at this. :(
        /// </summary>
        public static void CreateNotExisting()
        {
            /*
            System.Threading.Thread.Sleep(500);
            foreach (var item in FunctionPrototypeDefinitions.Collection.Items)
            {
                string functionType = "Function";
                string functionPrefix = item.FunctionPrefix;

                if (item.FunctionPrefix == "##")
                {
                    functionType = "Standard Function";
                }
                if (item.FunctionPrefix == "@@")
                {
                    functionType = "Instruction Function";
                }
                if (item.FunctionPrefix == "$$")
                {
                    functionType = "Scope Function";
                    functionPrefix = string.Empty;
                }

                string topic = $"Wiki Help :: {item.ProperName}";

                if (functionType == "Instruction Function")
                {
                    topic = $"Wiki Help :: {item.ProperName} - Instruction";
                }

                string navigation = CanonicalNavigation.CleanAndValidate(topic);

                var page = PageRepository.GetPageInfoByNavigation(navigation);
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
                    html.AppendLine($"The {item.ProperName} {functionType.ToLower()} is !!FILL_IN_THE_BLANK!!");
                    html.AppendLine("");
                    html.AppendLine("");
                    html.AppendLine("==Prototype");
                    html.Append($"##Color(${{keywordColor}}, **#{{ {functionPrefix}{item.ProperName} }}#**)");
                    if ((item.Value.Parameters?.Count ?? 0) == 0)
                    {
                        html.AppendLine("()");
                    }
                    else
                    {
                        html.Append("(");
                        foreach (var p in item.Value.Parameters)
                        {
                            html.Append($"##Color(${{keywordColor}}, {p.Type}{(p.IsInfinite ? ":Infinite" : "")})");
                            if (p.IsRequired)
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
                        html.Append(")");
                    }

                    html.AppendLine("");
                    html.AppendLine("");
                    html.AppendLine("");
                    html.AppendLine("===Parameters");
                    html.AppendLine("{{Bullets");

                    if (item.Value.Parameters.Count == 0)
                    {
                        html.AppendLine($"None.");
                    }

                    foreach (var p in item.Value.Parameters)
                    {
                        html.AppendLine($"**Name:** ##Color(${{identifierColor}}, {p.Name}) ##Color(${{metaColor}}, {(p.IsRequired ? "[Required]" : "{Optional}")})");
                        html.AppendLine($">**Type:** ##Color(${{keywordColor}}, {p.Type}{(p.IsInfinite ? ":Infinite" : "")})");
                        if (string.IsNullOrEmpty(p.DefaultValue) == false)
                        {
                            html.AppendLine($">**Default:** ##Color(${{identifierColor}}, {p.DefaultValue})");
                        }
                        if (p.AllowedValues != null)
                        {
                            html.AppendLine($">**Values:** ##Color(${{identifierColor}}, \"{string.Join(", ", p.AllowedValues)}\")");
                        }
                        html.AppendLine($">**Description:** !!FILL_IN_THE_BLANK!!");
                    }
                    html.AppendLine("}}");
                    html.AppendLine("");

                    html.AppendLine("==Examples");
                    html.AppendLine("{{Code(wiki)#{");


                    if (item.FunctionPrefix == "$$")
                    {
                        html.Append("{{ " + $"{item.ProperName}");
                        if ((item.Value.Parameters?.Count ?? 0) == 0)
                        {
                            html.AppendLine("()");
                        }
                        else
                        {
                            html.AppendLine($"({string.Join(", ", item.Value.Parameters.Select(o => o.Name))})");
                        }

                        html.AppendLine("This is the body content of the function scope.");

                        html.AppendLine("}}");
                    }
                    else
                    {
                        html.Append($"{item.FunctionPrefix}{item.ProperName}");
                        if ((item.Value.Parameters?.Count ?? 0) == 0)
                        {
                            html.AppendLine("()");
                        }
                        else
                        {
                            html.AppendLine($"({string.Join(", ", item.Value.Parameters.Select(o => o.Name))})");
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

                    page = new Page()
                    {
                        Navigation = navigation,
                        Name = topic,
                        Description = $"Documentation of the built-in {item.ProperName.ToLower()} {functionType.ToLower()} !!FILL_IN_THE_BLANK!!.",
                        CreatedByUserId = 1,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedByUserId = 1,
                        ModifiedDate = DateTime.UtcNow,
                        Body = html.ToString()
                    };

                    PageRepository.SavePage(page);
                }
            }
            */
        }
    }
}
