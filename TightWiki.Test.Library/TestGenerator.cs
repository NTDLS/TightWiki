using System.Reflection;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Module.Function;

namespace TightWiki.Test.Library
{
    public class TestGenerator
    {
        private readonly ITwEngine _engine;

        public TestGenerator(ITwEngine engine)
        {
            _engine = engine;
        }

        public void Generate(List<ITwFunctionDescriptor> collection)
        {
            //Generate all combination for required parameters.
            foreach (var prototype in collection)
            {
                var requiredParams = prototype.Parameters.Where(o => o.HasDefaultValue == false).ToList();

                var paramValueSets = requiredParams
                    .Select(p => GetValues(p))
                    .ToList();

                var combinations = GetCombinations(paramValueSets);

                foreach (var combo in combinations)
                {
                    var fArgs = string.Join(", ", combo.Select(FormatValue));
                    Console.WriteLine($"{prototype.FunctionAttribute.Demarcation}{prototype.FunctionAttribute.Name}({fArgs})");
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
            if (p.ParameterType.IsArray)
            {
                return new List<object?>();
            }


            return p.ParameterType switch
            {
                Type stringType when stringType == typeof(string) => new List<object?> { "test" },
                //Type infiniteStringType when infiniteStringType == typeof(string) => new List<object?> { "test1", "test2" },
                Type intType when intType == typeof(int) => new List<object?> { 0, 1 },
                Type doubleType when doubleType == typeof(double) => new List<object?> { 0.0 },
                Type boolType when boolType == typeof(bool) => new List<object?> { true },
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
