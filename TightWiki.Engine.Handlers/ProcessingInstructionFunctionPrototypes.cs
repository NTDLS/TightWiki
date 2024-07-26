namespace TightWiki.EngineFunction
{
    public static class ProcessingInstructionFunctionPrototypes
    {
        private static FunctionPrototypeCollection? _collection;

        public static FunctionPrototypeCollection Collection
        {
            get
            {
                if (_collection == null)
                {
                    _collection = new FunctionPrototypeCollection();

                    //Processing instructions:
                    _collection.Add("@@Deprecate:");
                    _collection.Add("@@Protect:<bool>{isSilent}='false'");
                    _collection.Add("@@Template:");
                    _collection.Add("@@Review:");
                    _collection.Add("@@NoCache:");
                    _collection.Add("@@Include:");
                    _collection.Add("@@Draft:");
                    _collection.Add("@@HideFooterComments:");
                    _collection.Add("@@HideFooterLastModified:");

                    //System functions:
                    _collection.Add("@@SystemEmojiCategoryList:");
                    _collection.Add("@@SystemEmojiList:");
                }

                return _collection;
            }
        }
    }
}
