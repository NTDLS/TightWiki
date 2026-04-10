using TightWiki.Test.Library;

namespace TightWiki.Test.CaseGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var artifacts = new MockWikiEngineArtifacts();

            var testGenerator = new TestGenerator(artifacts);

            //testGenerator.Generate(artifacts.Engine.ProcessingFunctions);
            testGenerator.Generate(artifacts.Engine.StandardFunctions);

            //testGenerator.Generate(artifacts.Engine.StandardFunctionHandler.Descriptors);
        }
    }
}
