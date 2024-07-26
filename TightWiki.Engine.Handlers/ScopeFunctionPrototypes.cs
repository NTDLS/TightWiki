using static TightWiki.EngineFunction.FunctionPrototypeCollection;

namespace TightWiki.EngineFunction
{
    public static class ScopeFunctionPrototypes
    {
        private static FunctionPrototypeCollection? _collection;

        public static FunctionPrototypeCollection Collection
        {
            get
            {
                if (_collection == null)
                {
                    _collection = new FunctionPrototypeCollection(WikiFunctionType.Scope);

                    //Scope functions:
                    _collection.Add("$$Code: <string>{language(auto,wiki,cpp,lua,graphql,swift,r,yaml,kotlin,scss,shell,vbnet,json,objectivec,perl,diff,wasm,php,xml,bash,csharp,css,go,ini,javascript,less,makefile,markdown,plaintext,python,python-repl,ruby,rust,sql,typescript)}='auto'");
                    _collection.Add("$$Bullets: <string>{type(unordered,ordered)}='unordered'");
                    _collection.Add("$$Order: <string>{direction(ascending,descending)}='ascending'");
                    _collection.Add("$$Jumbotron:");
                    _collection.Add("$$Callout: <string>{styleName(default,primary,secondary,success,info,warning,danger)}='default' | <string>{titleText}=''");
                    _collection.Add("$$Background: <string>{styleName(default,primary,secondary,light,dark,success,info,warning,danger,muted)}='default'");
                    _collection.Add("$$Foreground: <string>{styleName(default,primary,secondary,light,dark,success,info,warning,danger,muted)}='default'");
                    _collection.Add("$$Alert: <string>{styleName(default,primary,secondary,light,dark,success,info,warning,danger)}='default' | <string>{titleText}=''");
                    _collection.Add("$$Card: <string>{styleName(default,primary,secondary,light,dark,success,info,warning,danger)}='default' | <string>{titleText}=''");
                    _collection.Add("$$Collapse: <string>{linkText}='Show'");
                    _collection.Add("$$Table: <boolean>{hasBorder}='true' | <boolean>{isFirstRowHeader}='true'");
                    _collection.Add("$$StripedTable: <boolean>{hasBorder}='true' | <boolean>{isFirstRowHeader}='true'");
                    _collection.Add("$$DefineSnippet: <string>[name]");
                }

                return _collection;
            }
        }
    }
}
