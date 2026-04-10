using System.Collections.Immutable;
using System.Reflection;
using TightWiki.Library.Dummy;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Module.Function;

namespace TightWiki.Test.Library
{
    public class TestGenerator(MockWikiEngineArtifacts artifacts)
    {
        public void Generate(List<ITwFunctionDescriptor> collection)
        {
            var session = new TwDummySessionState();

            var outputPath = "C:\\NTDLS\\TightWiki\\TightWiki.Tests\\Markup";

            HashSet<string> generatedTests = new HashSet<string>();

            //Generate all combination for required parameters.
            foreach (var prototype in collection)
            {
                bool isScopeFunction = prototype.FunctionAttribute is TwScopeFunctionPluginAttribute;

                var requiredParams = prototype.Parameters
                    .Where(o => o.ParameterType != typeof(ITwEngineState) && o.HasDefaultValue == false).ToList();

                var optionalParams = prototype.Parameters
                    .Where(o => o.ParameterType != typeof(ITwEngineState) && o.HasDefaultValue == true).ToList();

                if (isScopeFunction)
                {
                    requiredParams = requiredParams.Skip(1).ToList();
                }

                // Build value sets for required params
                var requiredValueSets = requiredParams.Select(p => GetValues(p)).ToList();

                // For optional params, each one can be either omitted (sentinel) or one of its values.
                // We use a sentinel to mean "stop here" when building the arg list.
                var sentinel = new object();

                var optionalValueSets = optionalParams
                    .Select(p => new List<object?> { sentinel }.Concat(GetValues(p)).ToList())
                    .ToList();

                var allValueSets = requiredValueSets.Concat(optionalValueSets).ToList();

                var combinations = GetCombinations(allValueSets);

                foreach (var combo in combinations)
                {
                    var trimmedCombo = combo
                        .TakeWhile(v => v != sentinel)
                        .ToList();

                    var fArgs = string.Join(", ", trimmedCombo.Select(FormatValue));

                    string markup = $"{prototype.FunctionAttribute.Demarcation}{prototype.FunctionAttribute.Name}({fArgs})";

                    var body = $"##title\r\n##toc\r\n===Overview\r\n==Test Result\r\n{markup}\r\n";

                    if (isScopeFunction)
                    {
                        //Add the closing marker for the scope functions.
                        body += $"This is the body text for {prototype.FunctionAttribute.Name}\r\n";
                        body += "}}\r\n";
                    }

                    var page = artifacts.GetMockPage("Test", body);

                    int suffix = 1;
                    string testName = $"Test{prototype.FunctionAttribute.Name}_{suffix:D6}";
                    while (generatedTests.Contains(testName))
                    {
                        suffix++;
                        testName = $"Test{prototype.FunctionAttribute.Name}_{suffix:D6}";
                    }

                    generatedTests.Add(testName);

                    Console.WriteLine(testName);
                    //Console.WriteLine(markup);
                    var processed = artifacts.Engine.Transform(artifacts.Localizer, session, page);
                    //Console.WriteLine(processed.Result.HtmlResult);

                    var wikiPath = Path.Combine(outputPath, $"{testName}.wiki");
                    File.WriteAllText(wikiPath, body);
                    File.WriteAllText($"{wikiPath}.expected", processed.Result.HtmlResult);
                }
            }
        }

        private static IEnumerable<List<object?>> GetCombinations(List<List<object?>> sequences)
        {
            if (sequences.Count == 0)
                yield return new List<object?>();

            else
            {
                foreach (var item in sequences[0])
                {
                    foreach (var tail in GetCombinations(sequences.Skip(1).ToList()))
                    {
                        var result = new List<object?> { item };
                        result.AddRange(tail);
                        yield return result;
                    }
                }
            }
        }

        private static List<object?> GetValues(ParameterInfo p)
        {
            if (p.ParameterType.IsEnum)
            {
                return Enum.GetValues(p.ParameterType)?.Cast<object?>().ToList() ?? [];
            }

            if (p.ParameterType.IsArray)
            {
                return new List<object?> { "test1", "test2" };
            }

            return p.ParameterType switch
            {
                Type stringType when stringType == typeof(string) => new List<object?> { "test" },
                Type intType when intType == typeof(int) => new List<object?> { 0, 1, 3 },
                Type doubleType when doubleType == typeof(double) => new List<object?> { 0.5, 0.75, 1 },
                Type boolType when boolType == typeof(bool) => new List<object?> { true, false },
                _ => new List<object?> { null }
            };
        }

        private static string FormatValue(object? value)
        {
            return value switch
            {
                null => "null",
                string s => $"\"{s}\"",
                bool b => b.ToString().ToLower(),
                double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => value.ToString()
            } ?? throw new ArgumentException();
        }

    }
}
