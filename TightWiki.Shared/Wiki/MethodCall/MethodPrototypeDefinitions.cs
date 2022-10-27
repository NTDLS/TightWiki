namespace TightWiki.Shared.Wiki.MethodCall
{
    public static class MethodPrototypeDefinitions
    {
        private static MethodPrototypeCollection _methodPrototypes;

        public static MethodPrototype Get(string methodName)
        {
            return Collection.Get(methodName);
        }

        public static MethodPrototypeCollection Collection
        {
            get
            {
                if (_methodPrototypes == null)
                {
                    _methodPrototypes = new MethodPrototypeCollection();

                    _methodPrototypes.Add("code: <string>{language(auto,cpp,lua,graphql,swift,r,yaml,kotlin,scss,shell,vbnet,json,objectivec,perl,diff,wasm,php,xml,bash,csharp,css,go,ini,javascript,less,makefile,markdown,plaintext,python,python-repl,ruby,rust,sql,typescript)}='auto'");
                    _methodPrototypes.Add("bullets: <string>{type(unordered,ordered)}='unordered' | <string>{title}=''");
                    _methodPrototypes.Add("jumbotron:");
                    _methodPrototypes.Add("callout: <string>{style(default,primary,secondary,success,info,warning,danger)}='default' | <string>{title}=''");
                    _methodPrototypes.Add("background: <string>{style(default,primary,secondary,light,dark,success,info,warning,danger,muted)}='default'");
                    _methodPrototypes.Add("foreground: <string>{style(default,primary,secondary,light,dark,success,info,warning,danger,muted)}='default'");
                    _methodPrototypes.Add("alert: <string>{style(default,primary,secondary,light,dark,success,info,warning,danger)}='default' | <string>{title}=''");
                    _methodPrototypes.Add("card: <string>{style(default,primary,secondary,light,dark,success,info,warning,danger)}='default' | <string>{title}=''");
                    _methodPrototypes.Add("Tag: <string:infinite>[tags]");
                    _methodPrototypes.Add("TextList: <string:infinite>[tokens] | <int>{Top}='1000' | <string>{view(List,Full)}='Full'");
                    _methodPrototypes.Add("TagList: <string:infinite>[tags] | <int>{Top}='1000' | <string>{view(List,Full)}='Full'");
                    _methodPrototypes.Add("SearchCloud: <string:infinite>[tokens] | <int>{Top}='1000'");
                    _methodPrototypes.Add("TagGlossary: <string:infinite>[tags] | <int>{Top}='1000' | <string>{view(List,Full)}='Full'");
                    _methodPrototypes.Add("RecentlyModified: <int>{Top}='1000' | <string>{view(List,Full)}='Full'");
                    _methodPrototypes.Add("TextGlossary: <string:infinite>[tokens] | <int>{Top}='1000' | <string>{view(List,Full)}='Full'");
                    _methodPrototypes.Add("TagCloud: <string>[tag] | <int>{Top}='1000'");
                    _methodPrototypes.Add("Image: <string>[name] | <int>{scale}='100' | <string>{altText}=''");
                    _methodPrototypes.Add("File: <string>[name] | <string>[linkText] | <bool>{showSize}='false'");
                    _methodPrototypes.Add("Related: <int>{Top}='1000' | <string>{view(List,Flat,Full)}='Full'");
                    _methodPrototypes.Add("Tags: <string>{view(Flat,List)}='List'");
                    _methodPrototypes.Add("EditLink: <string>{linkText}='edit'");
                    _methodPrototypes.Add("Collpase: <string>{linkText}='Show'");
                    _methodPrototypes.Add("Inject: <string>[pageName]");
                    _methodPrototypes.Add("BR: <int>{Count}='1'");
                    _methodPrototypes.Add("NL: <int>{Count}='1'");
                    _methodPrototypes.Add("HR: <int>{Height}='1'");
                    _methodPrototypes.Add("NewLine: <int>{Count}='1'");
                    _methodPrototypes.Add("History:<string>{view(Full,List)}='Full' | <int>{pageSize}='5' | <bool>{pageSelector}='true'");
                    _methodPrototypes.Add("TOC:<bool>{alphabetized}='false'");
                    _methodPrototypes.Add("Title:");
                    _methodPrototypes.Add("Navigation:");
                    _methodPrototypes.Add("Name:");
                    _methodPrototypes.Add("Created:");
                    _methodPrototypes.Add("LastModified:");
                    _methodPrototypes.Add("Files:");
                    _methodPrototypes.Add("Depreciate:");
                    _methodPrototypes.Add("Protect:<bool>{isSilent}='false'");
                    _methodPrototypes.Add("Remplate:");
                    _methodPrototypes.Add("Review:");
                    _methodPrototypes.Add("Include:");
                    _methodPrototypes.Add("Draft:");
                    _methodPrototypes.Add("AppVersion:");
                }

                return _methodPrototypes;
            }
        }
    }
}
