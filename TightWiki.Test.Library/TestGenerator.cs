using TightWiki.Plugin.Interfaces;

namespace TightWiki.Test.Library
{
    public class TestGenerator
    {
        private readonly ITwEngine _engine;

        public TestGenerator(ITwEngine engine)
        {
            _engine = engine;
        }

        /*
        public void Generate(FunctionPrototypeCollection collection)
        {
            //Generate all combination for required parameters.
            foreach (var prototype in collection.Items)
            {
                var requiredParams = prototype.Parameters.Where(o => o.IsRequired).ToList();

                var paramValueSets = requiredParams
                    .Select(p => GetValues(p))
                    .ToList();

                var combinations = GetCombinations(paramValueSets);

                foreach (var combo in combinations)
                {
                    var fArgs = string.Join(", ", combo.Select(FormatValue));
                    Console.WriteLine($"{Descriptor.Demarcation}{prototype.ProperName}({fArgs})");
                }
            }

            //Generate all combination for ALL parameters.
            foreach (var prototype in collection.Items)
            {
                var paramValueSets = prototype.Parameters
                    .Select(p => GetValues(p))
                    .ToList();

                var combinations = GetCombinations(paramValueSets);

                foreach (var combo in combinations)
                {
                    var fArgs = string.Join(", ", combo.Select(FormatValue));
                    Console.WriteLine($"{prototype.Demarcation}{prototype.ProperName}({fArgs})");
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

        private static List<object?> GetValues(PrototypeParameter p)
        {
            if (p.AllowedValues.Any())
                return p.AllowedValues.Cast<object?>().ToList();

            return p.Type switch
            {
                WikiFunctionParamType.String => new List<object?> { "test" },
                WikiFunctionParamType.InfiniteString => new List<object?> { "test1", "test2" },
                WikiFunctionParamType.Integer => new List<object?> { 0, 1 },
                WikiFunctionParamType.Double => new List<object?> { 0.0, 1.5 },
                WikiFunctionParamType.Boolean => new List<object?> { true, false },
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
        */
    }
}
