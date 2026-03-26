using TightWiki.Test.Library;

namespace TightWiki.Test.CaseGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var artifacts = new MockWikiEngineArtifacts();

            var testGenerator = new TestGenerator(artifacts.Engine);

            testGenerator.Generate(artifacts.Engine.ScopeFunctionHandler.Prototypes);
            testGenerator.Generate(artifacts.Engine.StandardFunctionHandler.Prototypes);
        }
    }
}
