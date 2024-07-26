namespace TightWiki.EngineFunction
{
    public static class StandardPostProcessingFunctionPrototypes
    {
        private static FunctionPrototypeCollection? _collection;

        public static FunctionPrototypeCollection Collection
        {
            get
            {
                if (_collection == null)
                {
                    _collection = new FunctionPrototypeCollection();

                    //Standard functions:
                    _collection.Add("##Tags: <string>{styleName(Flat,List)}='List'");
                    _collection.Add("##TagCloud: <string>[pageTag] | <integer>{Top}='1000'");
                    _collection.Add("##SearchCloud: <string>[searchPhrase] | <integer>{Top}='1000'");
                    _collection.Add("##TOC:<bool>{alphabetized}='false'");
                }

                return _collection;
            }
        }
    }
}
